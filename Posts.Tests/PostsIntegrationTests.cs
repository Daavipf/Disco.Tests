using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using Disco.DTOs;
using Disco.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Disco.Tests;

public class PostsIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;
    private readonly ApiFactory _factory;

    public PostsIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _authenticatedClient = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetPosts_ShouldReturnSuccessAndPosts()
    {
        var response = await _authenticatedClient.GetAsync("/api/posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var posts = await response.Content.ReadFromJsonAsync<List<PostResponseDTO>>();
        Assert.NotNull(posts);
    }

    [Fact]
    public async Task CreateNewPost_ShouldReturnCreated()
    {
        var artistId = await GetArtistID();

        var newPost = new PostRequestDTO
        {
            Title = "Novo Post com UUID",
            Content = "Testando a relação com o artista",
            Artistid = artistId
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/posts", newPost);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetPostById_ShouldReturnPost_WhenPostExists()
    {
        var artistId = await GetArtistID();
        var postId = await CreatePostInDb("Post de Teste", artistId);

        var response = await _client.GetAsync($"/api/posts/{postId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var post = await response.Content.ReadFromJsonAsync<PostResponseDTO>();
        Assert.Equal(postId, post!.Id);
    }

    [Fact]
    public async Task PutPost_ShouldReturnNoContent_WhenUserIsAuthor()
    {
        var artistId = await GetArtistID();
        var postId = await CreatePostInDb("Título Original", artistId);

        var updatedPost = new PostRequestDTO
        {
            Title = "Título Atualizado",
            Content = "Conteúdo Atualizado",
            Artistid = artistId
        };

        var response = await _authenticatedClient.PutAsJsonAsync($"/api/posts/{postId}", updatedPost);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SoftDeletePost_ShouldReturnNoContent()
    {
        var artistId = await GetArtistID();
        var postId = await CreatePostInDb("Post para Deletar", artistId);

        var response = await _authenticatedClient.DeleteAsync($"/api/posts/{postId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ReactToPost_ShouldAddReaction()
    {
        var artistId = await GetArtistID();
        var postId = await CreatePostInDb("Post para Reação", artistId);
        var reaction = new ReactionDTO
        {
            PostId = postId,
            ReactionType = "Like"
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/posts/react", reaction);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains("Reação adicionada", result);
    }

    [Fact]
    public async Task GetPost_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/posts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutPost_ShouldReturnForbid_WhenUserIsNotTheAuthor()
    {
        var artistId = await GetArtistID();
        var postId = await CreatePostWithDifferentAuthor(artistId);

        var updateDto = new PostRequestDTO { Title = "Tentativa Hacker", Content = "...", Artistid = artistId };

        var response = await _authenticatedClient.PutAsJsonAsync($"/api/posts/{postId}", updateDto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeletePostHard_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        var artistId = await GetArtistID();
        var postId = await CreatePostInDb("Post Alvo", artistId);

        var response = await _authenticatedClient.DeleteAsync($"/api/posts/{postId}/hard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RestorePost_ShouldReturnBadRequest_WhenPostIsNotDeleted()
    {
        var artistId = await GetArtistID();
        var postId = await CreatePostInDb("Post Ativo", artistId);

        var response = await _authenticatedClient.PatchAsync($"/api/posts/{postId}/restore", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Este post não está deletado", content);
    }

    [Fact]
    public async Task ReactToPost_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        var reaction = new ReactionDTO { PostId = Guid.NewGuid(), ReactionType = "🔥" };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/posts/react", reaction);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> GetArtistID()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var artist = await db.Artists.FirstOrDefaultAsync(a => a.Name == "Beatles");
            var artistId = artist!.Id;

            return artistId;
        }
    }

    private async Task<Guid> CreatePostInDb(string title, Guid artistId)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await db.Users.FirstOrDefaultAsync();

            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = "Conteúdo de teste",
                Artistid = artistId,
                Authorid = user!.Id,
                Createdat = DateTime.UtcNow
            };

            db.Posts.Add(post);
            await db.SaveChangesAsync();
            return post.Id;
        }
    }

    private async Task<Guid> CreatePostWithDifferentAuthor(Guid artistId)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otherUser = new User { Id = Guid.NewGuid(), Name = "outro_usuario", Email = "outro@teste.com", Hashpassword = "abc-123" };
            db.Users.Add(otherUser);

            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Post de Outro",
                Content = "...",
                Artistid = artistId,
                Authorid = otherUser.Id
            };

            db.Posts.Add(post);
            await db.SaveChangesAsync();
            return post.Id;
        }
    }
}