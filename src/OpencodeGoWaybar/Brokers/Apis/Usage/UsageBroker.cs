using System.Net.Http.Headers;

namespace OpencodeGoWaybar.Brokers.Apis.Usage;

internal sealed class UsageBroker(HttpClient httpClient, Uri usageEndpoint) : IUsageBroker
{
    public async ValueTask<UsageApiBrokerResponse> GetUsageAsync(
        string apiKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, usageEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new UsageApiBrokerResponse(response.StatusCode, body);
    }
}
