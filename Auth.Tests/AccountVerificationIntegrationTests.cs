using Disco.DTOs;
using System.Net.Http.Json;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Disco.Models;
using Microsoft.EntityFrameworkCore;

namespace Disco.Tests;

public class AccountVerificationIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public AccountVerificationIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task TestVerifyAccountSignUp()
    {
        var signupDTO = new SignupDTO
        {
            Username = "suki",
            Email = "suki@email.com",
            Password = "Suki1234",
            ConfirmPassword = "Suki1234"
        };

        var signupResponse = await _client.PostAsJsonAsync("/api/auth/signup", signupDTO);
        Assert.Equal(HttpStatusCode.Created, signupResponse.StatusCode);

        var signupData = await signupResponse.Content.ReadFromJsonAsync<SignupResponseDTO>();
        Assert.NotNull(signupData);
        Assert.False(signupData.User.Isverified);

        var verificationToken = signupData.Token;
        var verifyResponse = await _client.PostAsync($"/api/auth/verify?token={verificationToken}", null);

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == signupDTO.Email);

            Assert.NotNull(user);
            Assert.True(user.Isverified);
        }
    }

    [Fact]
    public async Task TestInvalidTokenSignUp()
    {
        var verificationToken = "token-invalido";

        var response = await _client.PostAsync($"/api/auth/verify?token={verificationToken}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorMessage = await response.Content.ReadAsStringAsync();
        Assert.Equal("Token de verificação inválido.", errorMessage);
    }
}