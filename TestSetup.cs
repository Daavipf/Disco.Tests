using System;
using Microsoft.EntityFrameworkCore;
using Disco.Models;

public abstract class TestSetup : IDisposable
{
  protected readonly AppDbContext context;

  public TestSetup()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;

    context = new AppDbContext(options);

    SeedUser(context);

    context.Database.EnsureCreated();
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