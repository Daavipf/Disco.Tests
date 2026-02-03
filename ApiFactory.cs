using System.Data.Common;
using Disco.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                ["Jwt:Key"] = "uma_chave_super_secreta_com_pelo_menos_32_caracteres"
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
}