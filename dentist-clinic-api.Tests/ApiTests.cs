using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace dentist_clinic_api.Tests;

public class ApiTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RootEndpoint_Returns200Ok()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }
}