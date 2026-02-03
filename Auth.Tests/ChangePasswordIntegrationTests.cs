using Disco.DTOs;
using System.Net.Http.Json;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Disco.Models;
using Microsoft.EntityFrameworkCore;

namespace Disco.Tests;

public class ChangePasswordIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public ChangePasswordIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task TestChangePassword()
    {
        var forgotPasswordRequest = new ForgotPasswordDTO { Email = "teste@email.com" };
        var forgotResponse = await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotPasswordRequest);
        Assert.Equal(HttpStatusCode.NoContent, forgotResponse.StatusCode);

        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "teste@email.com");
            Assert.NotNull(user);
            token = user.Resetpasswordtoken;
        }

        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = token,
            Password = "Suki4321",
            ConfirmPassword = "Suki4321"
        };
        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password", resetPasswordRequest);
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var loginRequest = new LoginDTO { Email = "teste@email.com", Password = "Suki4321" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task TestConflictingPasswordsPassword()
    {
        var forgotPasswordRequest = new ForgotPasswordDTO { Email = "teste@email.com" };
        await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotPasswordRequest);

        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == "teste@email.com");
            token = user.Resetpasswordtoken;
        }

        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = token,
            Password = "Suki4321",
            ConfirmPassword = "Suki4322" // Divergente
        };
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", resetPasswordRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TestInvalidTokenPassword()
    {
        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = "token-invalido-qualquer",
            Password = "Suki4321",
            ConfirmPassword = "Suki4321"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", resetPasswordRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}