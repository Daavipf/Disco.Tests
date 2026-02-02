using Microsoft.Extensions.Configuration;
using Disco.Models;
using Disco.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace Disco.Tests;

public class AccountVerificationIntegrationTests : TestSetup
{
    private readonly AuthController authController;

    public AccountVerificationIntegrationTests()
    {
        authController = new AuthController(context, mockConfig.Object);
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

        var signupResult = await authController.Signup(signupDTO);

        var createdResult = Assert.IsType<CreatedResult>(signupResult);
        Assert.Equal(201, createdResult.StatusCode);

        var dadosRetorno = Assert.IsType<SignupResponseDTO>(createdResult.Value);
        Assert.Equal("suki@email.com", dadosRetorno.User.Email);
        Assert.False(dadosRetorno.User.Isverified);

        var verificationToken = dadosRetorno.Token;

        var verifyResult = await authController.VerifyAccount(verificationToken);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == signupDTO.Email);

        Assert.NotNull(user);
        Assert.True(user.Isverified);
    }

    [Fact]
    public async Task TestInvalidTokenSignUp()
    {
        var verificationToken = "token-invalido";

        var verifyResult = await authController.VerifyAccount(verificationToken);

        var badResult = Assert.IsType<BadRequestObjectResult>(verifyResult);
        Assert.Equal("Token de verificação inválido.", badResult.Value);
    }
}
