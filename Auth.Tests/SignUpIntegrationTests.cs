using Microsoft.Extensions.Configuration;
using Disco.Models;
using Disco.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace Disco.Tests;

public class SignUpIntegrationTests : TestSetup
{
    private readonly AuthController authController;

    public SignUpIntegrationTests()
    {
        authController = new AuthController(context, mockConfig.Object);
    }

    [Fact]
    public async Task TestCorrectSignUp()
    {
        var signupDTO = new SignupDTO
        {
            Username = "suki",
            Email = "suki@email.com",
            Password = "Suki1234",
            ConfirmPassword = "Suki1234"
        };

        var result = await authController.Signup(signupDTO);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);

        var dadosRetorno = Assert.IsType<SignupResponseDTO>(createdResult.Value);
        Assert.Equal("suki@email.com", dadosRetorno.User.Email);
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

        var result = await authController.Signup(signupDTO);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badResult.StatusCode);

        var errorMessage = Assert.IsType<string>(badResult.Value);

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

        var result = await authController.Signup(signupDTO);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badResult.StatusCode);

        var errorMessage = Assert.IsType<string>(badResult.Value);

        Assert.Equal("As senhas não conferem", errorMessage);
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

        var result = await authController.Signup(signupDTO);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badResult.StatusCode);
    }
}
