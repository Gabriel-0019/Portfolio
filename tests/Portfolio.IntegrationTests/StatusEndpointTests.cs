using System.Net;
using System.Net.Http.Json;
using Portfolio.Api.Controllers;
using Portfolio.IntegrationTests.Infrastructure;

namespace Portfolio.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class StatusEndpointTests(PortfolioApiFactory factory)
{
    [Fact]
    public async Task GetStatus_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StatusResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal("Portfolio.Api", body.Service);
    }

    [Fact]
    public async Task UnknownApiRoute_ReturnsProblemDetails404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
