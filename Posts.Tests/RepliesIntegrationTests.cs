using System.Net;
using System.Net.Http.Json;
using Disco.DTOs;
using Disco.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Disco.Tests;

public class RepliesIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;
    private readonly ApiFactory _factory;

    public RepliesIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _authenticatedClient = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetRepliesByPost_ShouldReturnSuccess()
    {
        var postId = await CreatePostInDb();
        await CreateReplyInDb(postId, null);

        var response = await _client.GetAsync($"/api/replies/Post/{postId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var replies = await response.Content.ReadFromJsonAsync<List<ReplyResponseDTO>>();
        Assert.NotNull(replies);
        Assert.NotEmpty(replies);
    }

    [Fact]
    public async Task CreateReply_ToPost_ShouldReturnCreated()
    {
        var postId = await CreatePostInDb();
        var newReply = new CreateReplyDTO
        {
            PostId = postId,
            Content = "Resposta direta ao post",
            ParentId = null
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/replies", newReply);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reply = await response.Content.ReadFromJsonAsync<ReplyResponseDTO>();
        Assert.Equal(postId, reply!.PostId);
    }

    [Fact]
    public async Task CreateReply_ToAnotherReply_ShouldCreateThread()
    {
        var postId = await CreatePostInDb();
        var parentId = await CreateReplyInDb(postId, null);

        var childReply = new CreateReplyDTO
        {
            PostId = postId,
            Content = "Isso é uma thread (resposta da resposta)",
            ParentId = parentId
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/replies", childReply);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReplyResponseDTO>();
        Assert.Equal(parentId, result!.ParentId);
    }

    [Fact]
    public async Task CreateReply_ShouldReturnBadRequest_WhenParentReplyBelongsToDifferentPost()
    {
        var postA = await CreatePostInDb();
        var postB = await CreatePostInDb();
        var replyFromPostA = await CreateReplyInDb(postA, null);

        var invalidReply = new CreateReplyDTO
        {
            PostId = postB,
            Content = "Tentando linkar resposta do Post A no Post B",
            ParentId = replyFromPostA
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/replies", invalidReply);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Inconsistência", content);
    }

    [Fact]
    public async Task PutReply_ShouldReturnForbidden_WhenUserIsNotAuthor()
    {
        var postId = await CreatePostInDb();
        var replyId = await CreateReplyWithDifferentAuthor(postId);

        var updateDto = new UpdateReplyDTO { Content = "Tentativa de edição" };

        var response = await _authenticatedClient.PutAsJsonAsync($"/api/replies/{replyId}", updateDto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReactToReply_ShouldToggleReaction()
    {
        var postId = await CreatePostInDb();
        var replyId = await CreateReplyInDb(postId, null);
        var reactionDto = new ReplyReactionDTO { ReplyId = replyId, ReactionType = "🔥" };

        // Adiciona
        var responseAdd = await _authenticatedClient.PostAsJsonAsync("/api/replies/react", reactionDto);
        Assert.Equal(HttpStatusCode.OK, responseAdd.StatusCode);

        // Remove (Toggle)
        var responseRemove = await _authenticatedClient.PostAsJsonAsync("/api/replies/react", reactionDto);
        var result = await responseRemove.Content.ReadFromJsonAsync<dynamic>();
        Assert.Contains("removida", responseRemove.Content.ReadAsStringAsync().Result);
    }

    [Fact]
    public async Task DeleteReply_ShouldSoftDelete_AndChangeContent()
    {
        var postId = await CreatePostInDb();
        var replyId = await CreateReplyInDb(postId, null);

        var response = await _authenticatedClient.DeleteAsync($"/api/replies/{replyId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/replies/{replyId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
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
            Title = "Post para Reply",
            Content = "...",
            Artistid = artist.Id,
            Authorid = user.Id
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return post.Id;
    }

    private async Task<Guid> CreateReplyInDb(Guid postId, Guid? parentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync();

        var reply = new Reply
        {
            Id = Guid.NewGuid(),
            Postid = postId,
            Parentid = parentId,
            Authorid = user.Id,
            Content = "Reply de teste",
            Createdat = DateTime.UtcNow
        };
        db.Replies.Add(reply);
        await db.SaveChangesAsync();
        return reply.Id;
    }

    private async Task<Guid> CreateReplyWithDifferentAuthor(Guid postId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var otherUser = new User { Id = Guid.NewGuid(), Name = "alvo", Email = "alvo@teste.com", Hashpassword = "123" };
        db.Users.Add(otherUser);

        var reply = new Reply
        {
            Id = Guid.NewGuid(),
            Postid = postId,
            Authorid = otherUser.Id,
            Content = "Não pode editar isso"
        };
        db.Replies.Add(reply);
        await db.SaveChangesAsync();
        return reply.Id;
    }
}