using System;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Disco.Models;
using Moq;

public abstract class TestSetup : IDisposable
{
  protected readonly AppDbContext context;
  protected readonly Mock<IConfiguration> mockConfig;

  public TestSetup()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;

    context = new AppDbContext(options);

    mockConfig = new Mock<IConfiguration>();
    mockConfig.Setup(c => c["Jwt:Key"]).Returns("uma_chave_super_secreta_com_pelo_menos_32_caracteres");

    context.Database.EnsureCreated();

    SeedUser(context);
  }

  public void Dispose()
  {
    context.Database.EnsureDeleted();
    context.Dispose();
  }

  private void SeedUser(AppDbContext context)
  {
    var senhaOriginal = "senha123";
    var hashSenha = BCrypt.Net.BCrypt.HashPassword(senhaOriginal);

    var user = new User
    {
      Name = "Teste User",
      Email = "teste@email.com",
      Hashpassword = hashSenha,
      Isverified = true,
    };

    context.Users.Add(user);
    context.SaveChanges();
  }
}