
using Bloggit.App.Posts.Domain.Entities;
using Bloggit.App.Posts.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Bloggit.Tests.Posts.Shared;

public class PostgresServerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postGresSqlContainer;

    public string ConnectionString => _postGresSqlContainer?.GetConnectionString() ?? throw new InvalidOperationException("Container not initialized");

    public async Task DisposeAsync()
    {
        if (_postGresSqlContainer != null)
            await _postGresSqlContainer.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        _postGresSqlContainer = new PostgreSqlBuilder()
            .WithDatabase("bloggit_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        await _postGresSqlContainer.StartAsync();

        await using var context = new DataContext(new DbContextOptionsBuilder<DataContext>()
            .UseNpgsql(ConnectionString)
            .Options);

        await context.Database.MigrateAsync();

        await SeedTestDataAsync(context);
    }

    private async Task SeedTestDataAsync(DataContext context)
    {
        await context.Posts.AddRangeAsync(
        [
            new PostEntity
            {
                Title = "Test Post 1",
                Content = "This is the content of test post 1.",
                AuthorId = "Author 1",
                DateCreated = DateTime.UtcNow
            },
            new PostEntity
            {
                Title = "Test Post 2",
                Content = "This is the content of test post 2.",
                AuthorId = "Author 2",
                DateCreated = DateTime.UtcNow
            },
            new PostEntity
            {
                Title = "Test Post 3",
                Content = "This is the content of test post 3.",
                AuthorId = "Author 3",
                DateCreated = DateTime.UtcNow
            },
            new PostEntity
            {
                Title = "Test Post 4",
                Content = "This is the content of test post 4.",
                AuthorId = "Author 4",
                DateCreated = DateTime.UtcNow
            },
            new PostEntity
            {
                Title = "Test Post 5",
                Content = "This is the content of test post 5.",
                AuthorId = "Author 5",
                DateCreated = DateTime.UtcNow
            }
        ]);
        
        await context.SaveChangesAsync();
    }
}
