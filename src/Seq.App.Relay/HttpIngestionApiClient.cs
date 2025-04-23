using System.Net;

namespace Seq.App.Relay;

class HttpIngestionApiClient : IngestionApiClient
{
    readonly HttpClient _httpClient = new();
    
    public override async Task<HttpStatusCode> SendBatchAsync(Uri ingestionEndpoint, string? apiKey, Stream batch, string contentType)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, ingestionEndpoint)
        {
            Content = new StreamContent(batch)
            {
                Headers = { ContentType = new(contentType) }
            }
        };
        
        if (apiKey is not null)
            message.Headers.Add("X-Seq-ApiKey", apiKey);

        var response = await _httpClient.SendAsync(message);
        return response.StatusCode;
    }
}
