using Seq.Apps;

// ReSharper disable UnusedAutoPropertyAccessor.Global, MemberCanBePrivate.Global

namespace Seq.App.Relay;

[SeqApp("Relay",
    Description = "A simple HTTP/JSON forwarder that preserves all event metadata.")]
public sealed class RelayApp : SeqApp, ISubscribeToJsonAsync, IAsyncDisposable
{
    readonly IngestionApiClient _apiClient;
    IngestionRelay? _relay;

    [SeqAppSetting(DisplayName = "Target URL",
        HelpText = "The base address of the Seq instance to relay events to.")]
    public string? TargetUrl { get; set; }
        
    [SeqAppSetting(InputType = SettingInputType.Password, IsOptional = true, DisplayName = "API Key",
        HelpText = "An optional Seq API key to use when connecting to the target server.")]
    public string? ApiKey { get; set; }

    public RelayApp()
        : this(new HttpIngestionApiClient())
    {
    }
    
    internal RelayApp(IngestionApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    protected override void OnAttached()
    {
        _relay = new IngestionRelay(_apiClient, TargetUrl!, ApiKey, Log);
    }

    public async Task OnAsync(string json)
    {
        await _relay!.SendAsync(json);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_relay is not null)
            await _relay.DisposeAsync();
    }
}