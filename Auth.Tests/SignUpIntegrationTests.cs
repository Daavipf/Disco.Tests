using Disco.DTOs;
using System.Net.Http.Json;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Disco.Tests;

public class SignUpIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public SignUpIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TestCorrectSignup()
    {
        var signupDTO = new SignupDTO
        {
            Username = "suki",
            Email = "suki@email.com",
            Password = "Suki1234",
            ConfirmPassword = "Suki1234"
        };

        var response = await _client.PostAsJsonAsync<SignupDTO>("/api/auth/signup", signupDTO);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseData = await response.Content.ReadFromJsonAsync<SignupResponseDTO>();
        Assert.Equal("suki@email.com", responseData.User.Email);
        Assert.NotNull(responseData.Token);
    }

    [Fact]
    public async Task TestExistingAccountSignUp()
    {
        var signupDTO = new SignupDTO
        {
            Username = "suki",
            Email = "teste@email.com",
            Password = "Suki1234",
            ConfirmPassword = "Suki1234"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", signupDTO);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorMessage = await response.Content.ReadAsStringAsync();
        Assert.Equal("Já existe um usuário cadastrado com este e-mail.", errorMessage);
    }

    [Fact]
    public async Task TestConflictingPasswordsSignUp()
    {
        var signupDTO = new SignupDTO
        {
            Username = "suki",
            Email = "suki@email.com",
            Password = "Suki1234",
            ConfirmPassword = "Suki1233"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", signupDTO);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TestMissingFieldsSignUp()
    {
        var signupDTO = new SignupDTO
        {
            Username = "suki",
            Email = "suki@email.com",
            Password = "Suki1234",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", signupDTO);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}