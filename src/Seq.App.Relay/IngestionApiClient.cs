using System.Net;

namespace Seq.App.Relay;

abstract class IngestionApiClient
{
    public abstract Task<HttpStatusCode> SendBatchAsync(Uri ingestionEndpoint, string? apiKey, Stream batch, string contentType);
}
