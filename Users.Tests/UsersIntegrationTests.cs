using System.Net;
using System.Net.Http.Json;
using Disco.DTOs;
using Disco.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Disco.Tests;

public class UsersIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;
    private readonly ApiFactory _factory;

    public UsersIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _authenticatedClient = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnSuccess()
    {
        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserDTO>>();
        Assert.NotNull(users);
    }

    [Fact]
    public async Task PutUser_ShouldUpdate_WhenIsOwnProfile()
    {
        var userId = await GetAuthenticatedUserId();

        var updateDto = new UserDTO
        {
            Id = userId,
            Name = "Nome Atualizado",
            Email = "teste@email.com",
            Bio = "Nova Bio"
        };

        var response = await _authenticatedClient.PutAsJsonAsync($"/api/users/{userId}", updateDto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var userInDb = await GetUserFromDb(userId);
        Assert.Equal("Nome Atualizado", userInDb!.Name);
    }

    [Fact]
    public async Task PutUser_ShouldReturnForbid_WhenTryingToEditOtherUserProfile()
    {
        var otherUserId = await CreateOtherUserInDb();
        var updateDto = new UserDTO
        {
            Id = otherUserId,
            Name = "Hacker",
            Email = "teste@email.com",
            Bio = "Tentando mudar o que não deve"
        };

        var response = await _authenticatedClient.PutAsJsonAsync($"/api/users/{otherUserId}", updateDto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateAccount_ShouldSetDeletedAt_ForCurrentUser()
    {
        var response = await _authenticatedClient.DeleteAsync("/api/users/me/deactivate");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var userId = await GetAuthenticatedUserId();
        var userInDb = await GetUserFromDb(userId);

        Assert.NotNull(userInDb!.Deletedat);
    }

    [Fact]
    public async Task PostUser_ShouldReturnForbidden_ForNonAdmin()
    {
        var newUser = new User { Name = "Novo", Email = "novo@teste.com" };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/users", newUser);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Helpers
    private async Task<Guid> GetAuthenticatedUserId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == "teste@email.com");
        return user.Id;
    }

    private async Task<User?> GetUserFromDb(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
    }

    private async Task<Guid> CreateOtherUserInDb()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Outro",
            Email = $"outro{Guid.NewGuid()}@teste.com",
            Hashpassword = "123"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}