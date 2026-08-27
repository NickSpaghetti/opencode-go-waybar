using System.Net.Http.Headers;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Brokers.Usages;

internal sealed class UsageBroker(HttpClient httpClient, OpenCodeGoOptions options) : IUsageBroker
{
    private Uri UsageEndpoint => options.UsageEndpoint;

    public async ValueTask<UsageApiBrokerResponse> GetUsageAsync(
        string apiKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UsageEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new UsageApiBrokerResponse(response.StatusCode, body);
    }
}
