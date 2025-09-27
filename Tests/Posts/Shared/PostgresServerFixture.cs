
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
            // Posts for the test user (test-user-id)
            new PostEntity
            {
                Title = "Test User Post 1",
                Content = "This is the content of test user post 1.",
                AuthorId = "test-user-id",
                DateCreated = DateTime.UtcNow.AddDays(-2)
            },
            new PostEntity
            {
                Title = "Test User Post 2", 
                Content = "This is the content of test user post 2.",
                AuthorId = "test-user-id",
                DateCreated = DateTime.UtcNow.AddDays(-1)
            },
            // Posts for other users (to test tenant isolation)
            new PostEntity
            {
                Title = "Other User Post 1",
                Content = "This is the content of other user post 1.",
                AuthorId = "other-user-1",
                DateCreated = DateTime.UtcNow
            },
            new PostEntity
            {
                Title = "Other User Post 2",
                Content = "This is the content of other user post 2.",
                AuthorId = "other-user-2",
                DateCreated = DateTime.UtcNow
            },
            new PostEntity
            {
                Title = "Other User Post 3",
                Content = "This is the content of other user post 3.",
                AuthorId = "other-user-3",
                DateCreated = DateTime.UtcNow
            }
        ]);
        
        await context.SaveChangesAsync();
    }
}
