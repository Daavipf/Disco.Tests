using Microsoft.Extensions.Configuration;
using Disco.Models;
using Disco.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace Disco.Tests;

public class ChangePasswordIntegrationTests : TestSetup
{
    private readonly AuthController authController;

    public ChangePasswordIntegrationTests()
    {
        authController = new AuthController(context, mockConfig.Object);
    }

    [Fact]
    public async Task TestChangePassword()
    {
        var forgotPasswordRequest = new ForgotPasswordDTO
        {
            Email = "teste@email.com"
        };

        var request = await authController.ForgotPassword(forgotPasswordRequest);
        var response = Assert.IsType<NoContentResult>(request);
        Assert.Equal(204, response.StatusCode);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == forgotPasswordRequest.Email);
        Assert.NotNull(user);

        var token = user.Resetpasswordtoken;

        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = token,
            Password = "Suki4321",
            ConfirmPassword = "Suki4321"
        };

        request = await authController.ResetPassword(resetPasswordRequest);
        var newResponse = Assert.IsType<OkObjectResult>(request);
        Assert.Equal(200, newResponse.StatusCode);

        var loginRequest = await authController.Login(new LoginDTO { Email = "teste@email.com", Password = "Suki4321" });
        var loginResponse = Assert.IsType<OkObjectResult>(loginRequest);
    }

    [Fact]
    public async Task TestConflictingPasswordsPassword()
    {
        var forgotPasswordRequest = new ForgotPasswordDTO
        {
            Email = "teste@email.com"
        };

        var request = await authController.ForgotPassword(forgotPasswordRequest);
        var response = Assert.IsType<NoContentResult>(request);
        Assert.Equal(204, response.StatusCode);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == forgotPasswordRequest.Email);
        Assert.NotNull(user);

        var token = user.Resetpasswordtoken;

        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = token,
            Password = "Suki4321",
            ConfirmPassword = "Suki4322"
        };

        request = await authController.ResetPassword(resetPasswordRequest);
        var newResponse = Assert.IsType<BadRequestObjectResult>(request);
        Assert.Equal(400, newResponse.StatusCode);
    }

    [Fact]
    public async Task TestInvalidTokenPassword()
    {
        var forgotPasswordRequest = new ForgotPasswordDTO
        {
            Email = "teste@email.com"
        };

        var request = await authController.ForgotPassword(forgotPasswordRequest);
        var response = Assert.IsType<NoContentResult>(request);
        Assert.Equal(204, response.StatusCode);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == forgotPasswordRequest.Email);
        Assert.NotNull(user);

        var token = "invalid-token";

        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = token,
            Password = "Suki4321",
            ConfirmPassword = "Suki4321"
        };

        request = await authController.ResetPassword(resetPasswordRequest);
        var newResponse = Assert.IsType<BadRequestObjectResult>(request);
        Assert.Equal(400, newResponse.StatusCode);
    }
}
