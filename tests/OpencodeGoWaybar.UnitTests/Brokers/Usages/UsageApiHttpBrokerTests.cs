using System.Net;
using System.Net.Http.Headers;
using OpencodeGoWaybar.Brokers.Usages;
using Xunit;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.UnitTests.Brokers.Usages;

public sealed class UsageBrokerTests
{
    [Fact]
    public async Task ShouldSendBearerKeyAndReturnUsageResponseAsync()
    {
        // given
        using var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://mock.test/v1/usage", request.RequestUri!.ToString());
            Assert.Equal("test-key", request.Headers.Authorization!.Parameter);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "usage": {
                        "rolling": { "status": "ok", "percent": 12, "resetsAt": "2026-08-15T19:29:58Z" },
                        "weekly": { "status": "ok", "percent": 24, "resetsAt": "2026-08-17T00:00:00Z" },
                        "monthly": { "status": "ok", "percent": 36, "resetsAt": "2026-09-15T00:00:00Z" }
                      }
                    }
                    """, System.Text.Encoding.UTF8, "application/json")
            });
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://mock.test") };
        var broker = new UsageBroker(
            client,
            new OpenCodeGoOptions { UsageEndpoint = new Uri("https://mock.test/v1/usage") });

        var response = await broker.GetUsageAsync("test-key", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"percent\": 24", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedStatusToFoundationAsync()
    {
        // given
        using var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        using var client = new HttpClient(handler);
        var broker = new UsageBroker(
            client,
            new OpenCodeGoOptions { UsageEndpoint = new Uri("https://mock.test/v1/usage") });

        // when
        var response = await broker.GetUsageAsync("test-key", CancellationToken.None);

        // then
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
