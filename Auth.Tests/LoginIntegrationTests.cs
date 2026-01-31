using Microsoft.Extensions.Configuration;
using Disco.Models;
using Disco.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace Disco.Tests;

public class LoginIntegrationTests : TestSetup
{
    private readonly AuthController authController;

    public LoginIntegrationTests()
    {
        authController = new AuthController(context, mockConfig.Object);
    }

    [Fact]
    public async Task TestCorrectLogin()
    {
        var senhaOriginal = "senha123";

        var loginDto = new LoginDTO { Email = "teste@email.com", Password = senhaOriginal };

        var result = await authController.Login(loginDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var dadosRetorno = Assert.IsType<LoginResponseDTO>(okResult.Value);
        Assert.NotNull(dadosRetorno.Token);
        Assert.Equal("teste@email.com", dadosRetorno.User.Email);
    }

    [Fact]
    public async Task TestIncorrectPasswordLogin()
    {
        var senhaOriginal = "senha321";
        var loginDto = new LoginDTO { Email = "teste@email.com", Password = senhaOriginal };

        var result = await authController.Login(loginDto);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task TestNonExistentEmailLogin()
    {
        var senhaOriginal = "senha123";

        var loginDto = new LoginDTO { Email = "wrong@email.com", Password = senhaOriginal };

        var result = await authController.Login(loginDto);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }
}
