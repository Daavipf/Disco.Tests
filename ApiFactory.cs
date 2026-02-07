using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Disco.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Disco.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "S3cREt_K3Y@519237.901.132.13:0001"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb_");
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            SeedData(db);
        });
    }

    private void SeedData(AppDbContext context)
    {
        var hashSenha = BCrypt.Net.BCrypt.HashPassword("senha123");
        context.Users.Add(new User { Name = "Teste User", Email = "teste@email.com", Hashpassword = hashSenha, Isverified = true });
        context.Artists.Add(new Artist { Name = "Beatles", Bio = "Melhor banda", Avatar = "http://url.com" });
        context.SaveChanges();
    }

    public HttpClient CreateAuthenticatedClient(string email = "teste@email.com")
    {
        var client = CreateClient();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
            throw new InvalidOperationException($"O usuário com email '{email}' não foi encontrado no SeedData.");

        var token = GenerateJwtToken(user);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private string GenerateJwtToken(User user)
    {
        var key = Encoding.ASCII.GetBytes("S3cREt_K3Y@519237.901.132.13:0001");

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, "USER"),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}