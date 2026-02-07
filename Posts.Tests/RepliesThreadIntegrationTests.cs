using System.Net;
using System.Net.Http.Json;
using Disco.DTOs;
using Disco.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Disco.Tests;

public class RepliesThreadIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;
    private readonly ApiFactory _factory;

    public RepliesThreadIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _authenticatedClient = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetRepliesByPost_ShouldMaintainThreadHierarchy()
    {
        var postId = await CreatePostInDb();

        var replyL1 = await CreateReplyViaApi(postId, null, "Resposta Nível 1");

        var replyL2 = await CreateReplyViaApi(postId, replyL1.Id, "Resposta Nível 2 - Filho de L1");

        var replyL3 = await CreateReplyViaApi(postId, replyL2.Id, "Resposta Nível 3 - Neto de L1");

        var response = await _client.GetAsync($"/api/replies/Post/{postId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var thread = await response.Content.ReadFromJsonAsync<List<ReplyResponseDTO>>();

        Assert.NotNull(thread);
        Assert.Equal(3, thread.Count);

        var l1FromApi = thread.First(r => r.Id == replyL1.Id);
        var l2FromApi = thread.First(r => r.Id == replyL2.Id);
        var l3FromApi = thread.First(r => r.Id == replyL3.Id);

        Assert.Null(l1FromApi.ParentId);
        Assert.Equal(l1FromApi.Id, l2FromApi.ParentId);
        Assert.Equal(l2FromApi.Id, l3FromApi.ParentId);

        Assert.All(thread, r => Assert.Equal(postId, r.PostId));
    }

    private async Task<ReplyResponseDTO> CreateReplyViaApi(Guid postId, Guid? parentId, string content)
    {
        var dto = new CreateReplyDTO
        {
            PostId = postId,
            ParentId = parentId,
            Content = content
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/replies", dto);
        return (await response.Content.ReadFromJsonAsync<ReplyResponseDTO>())!;
    }

    private async Task<Guid> CreatePostInDb()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var artist = await db.Artists.FirstAsync();
        var user = await db.Users.FirstAsync();

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Post de Discussão",
            Content = "Iniciando a thread...",
            Artistid = artist.Id,
            Authorid = user.Id,
            Createdat = DateTime.UtcNow
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return post.Id;
    }
}