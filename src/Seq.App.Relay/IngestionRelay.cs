using System.Text;
using Serilog;

namespace Seq.App.Relay;

class IngestionRelay: IAsyncDisposable
{
    readonly IngestionApiClient _apiClient;
    readonly string? _apiKey;
    readonly ILogger _diagnosticLog;
    readonly Uri _ingestionEndpoint;
    const string ContentType = "application/vnd.serilog.clef";
    readonly Encoding _encoding = new UTF8Encoding(false);
    const int BatchSizeLimit = 1024 * 1024;

    readonly Task _relayBatches;
    readonly CancellationTokenSource _done = new();

    // The public API of this type is single-threaded; the lock is used for synchronization
    // between the background and foreground thread.
    readonly object _sync = new();
    TaskCompletionSource _bufferingTask = new();
    MemoryStream _bufferingBatch = new();

    public IngestionRelay(IngestionApiClient apiClient, string targetUrl, string? apiKey, ILogger diagnosticLog)
    {
        _apiClient = apiClient;
        _apiKey = apiKey;
        _diagnosticLog = diagnosticLog;
        _ingestionEndpoint = new(
            new Uri(targetUrl.EndsWith('/') ? targetUrl : $"{targetUrl}/"),
            "ingest/clef");

        _relayBatches = Task.Run(async () => await RelayBatchesAsync());
    }

    public async ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            _done.Cancel();
        }

        await _relayBatches;
    }

    public async Task SendAsync(string json)
    {
        Task wait;
        lock (_sync)
        {
            if (_done.IsCancellationRequested)
                return;

            var writer = new StreamWriter(_bufferingBatch, _encoding);
            writer.WriteLine(json);
            writer.Flush();

            if (_bufferingBatch.Length < BatchSizeLimit)
                return;

            wait = _bufferingTask.Task;
        }

        // Backpressure
        await wait;
    }

    async Task RelayBatchesAsync()
    {
        while (true)
        {
            TaskCompletionSource? batchCompletionSource = null;
            MemoryStream? batch = null;
            
            lock (_sync)
            {
                if (_bufferingBatch.Length != 0)
                {
                    batchCompletionSource = _bufferingTask;
                    _bufferingTask = new();
                    batch = _bufferingBatch;
                    _bufferingBatch = new();
                }
                else if (_done.IsCancellationRequested)
                {
                    return;
                }
            }

            if (batchCompletionSource is null || batch is null)
            {
                try
                {
                    await Task.Delay(100, _done.Token);
                }
                catch
                {
                    // Cancellation
                }
                
                continue;
            }

            await RelayBatchAsync(batch);
            batchCompletionSource.SetResult();
        }
    }

    async Task RelayBatchAsync(MemoryStream batch)
    {
        var tries = 0;
        while (true)
        {
            tries += 1;
            try
            {
                batch.Position = 0;
                var statusCode = await _apiClient.SendBatchAsync(_ingestionEndpoint, _apiKey, batch, ContentType);

                switch ((int)statusCode)
                {
                    case >= 200 and < 300:
                        return;
                    case >= 400 and < 500:
                        _diagnosticLog.Error("Dropping batch, status code {StatusCode}/{StatusDescription}", (int)statusCode, statusCode);
                        return;
                }

                if (tries == 3 || _done.IsCancellationRequested)
                {
                    _diagnosticLog.Error("Dropping batch after {Tries} attempts to relay, status code {StatusCode}/{StatusDescription} (stopping: {Stopping})", tries, (int)statusCode, statusCode, _done.IsCancellationRequested);
                    return;
                }

                await Task.Delay(15_000 * tries, _done.Token);
            }
            catch (Exception ex)
            {
                if (tries == 3 || _done.IsCancellationRequested)
                {
                    _diagnosticLog.Error(ex, "Dropping batch after {Tries} attempts to relay (stopping: {Stopping})", tries, _done.IsCancellationRequested);
                    return;
                }
            }
        }
    }
}