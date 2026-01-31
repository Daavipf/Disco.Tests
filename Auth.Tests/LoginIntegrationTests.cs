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
    [Fact]
    public async Task TestCorrectLogin()
    {
        var senhaOriginal = "senha123";

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Jwt:Key"]).Returns("uma_chave_super_secreta_com_pelo_menos_32_caracteres");

        var controller = new AuthController(context, mockConfig.Object);
        var loginDto = new LoginDTO { Email = "teste@email.com", Password = senhaOriginal };

        var result = await controller.Login(loginDto);

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

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Jwt:Key"]).Returns("uma_chave_super_secreta_com_pelo_menos_32_caracteres");

        var controller = new AuthController(context, mockConfig.Object);
        var loginDto = new LoginDTO { Email = "teste@email.com", Password = senhaOriginal };

        var result = await controller.Login(loginDto);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task TestNonExistentEmailLogin()
    {
        var senhaOriginal = "senha123";

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Jwt:Key"]).Returns("uma_chave_super_secreta_com_pelo_menos_32_caracteres");

        var controller = new AuthController(context, mockConfig.Object);
        var loginDto = new LoginDTO { Email = "wrong@email.com", Password = senhaOriginal };

        var result = await controller.Login(loginDto);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }
}
