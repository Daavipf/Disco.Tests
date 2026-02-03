using Disco.DTOs;
using System.Net.Http.Json;
using System.Net;

namespace Disco.Tests;

public class LoginIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public LoginIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TestCorrectLogin()
    {
        var loginDto = new LoginDTO { Email = "teste@email.com", Password = "senha123" };

        var response = await _client.PostAsJsonAsync<LoginDTO>("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseData = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(responseData.Token);
        Assert.Equal("teste@email.com", responseData.User.Email);
    }

    [Fact]
    public async Task TestIncorrectPasswordLogin()
    {
        var loginDto = new LoginDTO { Email = "teste@email.com", Password = "senha321" };

        var response = await _client.PostAsJsonAsync<LoginDTO>("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TestNonExistentEmailLogin()
    {
        var loginDto = new LoginDTO { Email = "wrong@email.com", Password = "senha123" };

        var response = await _client.PostAsJsonAsync<LoginDTO>("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TestMissingFieldsLogin()
    {
        var loginDto = new LoginDTO { Email = "wrong@email.com" };

        var response = await _client.PostAsJsonAsync<LoginDTO>("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}