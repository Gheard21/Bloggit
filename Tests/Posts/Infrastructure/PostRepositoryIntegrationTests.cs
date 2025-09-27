using Bloggit.App.Posts.Domain.Entities;
using Bloggit.App.Posts.Infrastructure;
using Bloggit.App.Posts.Infrastructure.Repositories;
using Bloggit.Tests.Posts.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bloggit.Tests.Posts.Infrastructure;

[Trait("Category", "Integration")]
public class PostRepositoryIntegrationTests(PostgresServerFixture fixture) : IClassFixture<PostgresServerFixture>
{
    private readonly PostgresServerFixture _fixture = fixture;

    [Fact]
    public async Task CreateAsync_ShouldAddPostToDatabase()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        
        var post = new PostEntity
        {
            Title = "Integration Test Post",
            Content = "This is a test post content",
            AuthorId = "test-author-123",
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = await repository.AddAsync(post);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id != Guid.Empty);
        
        // Verify it's actually in the database
        var savedPost = await context.Posts.FindAsync(result.Id);
        Assert.NotNull(savedPost);
        Assert.Equal("Integration Test Post", savedPost.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPost_WhenExists()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        
        // Seed test data
        var post = new PostEntity
        {
            Title = "Test Post for Retrieval",
            Content = "Content for retrieval test",
            AuthorId = "author-456",
            DateCreated = DateTime.UtcNow
        };
        
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(post.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Post for Retrieval", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenPostDoesNotExist()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Update_ShouldUpdateExistingPost()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        
        // Seed test data
        var post = new PostEntity
        {
            Title = "Original Title",
            Content = "Original Content",
            AuthorId = "author-789",
            DateCreated = DateTime.UtcNow
        };
        
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        // Modify the post
        post.Title = "Updated Title";
        post.Content = "Updated Content";

        // Act
        repository.Update(post);

        // Assert
        var updatedPost = await context.Posts.FindAsync(post.Id);
        Assert.NotNull(updatedPost);
        Assert.Equal("Updated Title", updatedPost.Title);
        Assert.Equal("Updated Content", updatedPost.Content);
    }

    [Fact]
    public async Task Delete_ShouldRemovePostFromDatabase()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        
        // Seed test data
        var post = new PostEntity
        {
            Title = "Post to be deleted",
            Content = "This post will be deleted",
            AuthorId = "author-delete",
            DateCreated = DateTime.UtcNow
        };
        
        context.Posts.Add(post);
        await context.SaveChangesAsync();
        var postId = post.Id;

        // Act
        await repository.Delete(post);

        // Assert
        var deletedPost = await context.Posts.FindAsync(postId);
        Assert.Null(deletedPost);
    }

    [Fact]
    public async Task Add_ShouldAddPostToContext_WithoutSaving()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        
        var post = new PostEntity
        {
            Title = "Test Post for Sync Add",
            Content = "This post is added synchronously",
            AuthorId = "sync-author",
            DateCreated = DateTime.UtcNow
        };

        // Act
        repository.Add(post);

        // Assert - Post should be in the context but not saved to database yet
        Assert.True(post.Id != Guid.Empty); // EF should have assigned an ID
        Assert.Contains(post, context.Posts.Local);
        
        // Verify it's NOT persisted to database yet by creating a new context
        await using var freshContext = CreateContext();
        var savedPost = await freshContext.Posts.FindAsync(post.Id);
        Assert.Null(savedPost);

        // Save and verify it's now in the database
        await repository.SaveChangesAsync();
        savedPost = await freshContext.Posts.FindAsync(post.Id);
        Assert.NotNull(savedPost);
        Assert.Equal("Test Post for Sync Add", savedPost.Title);
    }

    [Fact]
    public async Task Remove_ShouldMarkPostForDeletion_WithoutSaving()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        
        // Seed test data
        var post = new PostEntity
        {
            Title = "Post for sync removal",
            Content = "This post will be removed synchronously",
            AuthorId = "remove-author",
            DateCreated = DateTime.UtcNow
        };
        
        context.Posts.Add(post);
        await context.SaveChangesAsync();
        var postId = post.Id;

        // Act
        repository.Remove(post);

        // Assert - Post should still exist in database (not saved yet)
        var stillExistsInDb = await context.Posts.FindAsync(postId);
        Assert.NotNull(stillExistsInDb);

        // Save changes and verify it's now deleted
        await repository.SaveChangesAsync();
        var deletedPost = await context.Posts.FindAsync(postId);
        Assert.Null(deletedPost);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistMultipleOperations()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        
        var post1 = new PostEntity
        {
            Title = "First Post",
            Content = "Content 1",
            AuthorId = "batch-author",
            DateCreated = DateTime.UtcNow
        };
        
        var post2 = new PostEntity
        {
            Title = "Second Post",
            Content = "Content 2",
            AuthorId = "batch-author",
            DateCreated = DateTime.UtcNow
        };

        // Act - Add multiple posts without auto-saving
        repository.Add(post1);
        repository.Add(post2);
        
        // Verify nothing is saved yet
        var countBefore = await context.Posts.CountAsync();
        
        // Save all changes at once
        await repository.SaveChangesAsync();

        // Assert
        var countAfter = await context.Posts.CountAsync();
        Assert.True(countAfter >= countBefore + 2);
        
        var savedPost1 = await context.Posts.FindAsync(post1.Id);
        var savedPost2 = await context.Posts.FindAsync(post2.Id);
        
        Assert.NotNull(savedPost1);
        Assert.NotNull(savedPost2);
        Assert.Equal("First Post", savedPost1.Title);
        Assert.Equal("Second Post", savedPost2.Title);
    }

    [Fact]
    public async Task GetByIdAndAuthorAsync_ShouldReturnPost_WhenUserOwnsPost()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        var authorId = "test-author-123";
        
        var post = new PostEntity
        {
            Title = "User's Post",
            Content = "This post belongs to the user",
            AuthorId = authorId,
            DateCreated = DateTime.UtcNow
        };
        
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAndAuthorAsync(post.Id, authorId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("User's Post", result.Title);
        Assert.Equal(authorId, result.AuthorId);
    }

    [Fact]
    public async Task GetByIdAndAuthorAsync_ShouldReturnNull_WhenUserDoesNotOwnPost()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        var originalAuthorId = "original-author";
        var differentAuthorId = "different-author";
        
        var post = new PostEntity
        {
            Title = "Someone Else's Post",
            Content = "This post belongs to another user",
            AuthorId = originalAuthorId,
            DateCreated = DateTime.UtcNow
        };
        
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAndAuthorAsync(post.Id, differentAuthorId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByAuthorAsync_ShouldReturnOnlyUsersPosts()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        var userAuthorId = "target-user";
        var otherAuthorId = "other-user";
        
        // Create posts for target user
        var userPost1 = new PostEntity
        {
            Title = "User Post 1",
            Content = "Content 1",
            AuthorId = userAuthorId,
            DateCreated = DateTime.UtcNow.AddMinutes(-10)
        };
        
        var userPost2 = new PostEntity
        {
            Title = "User Post 2", 
            Content = "Content 2",
            AuthorId = userAuthorId,
            DateCreated = DateTime.UtcNow.AddMinutes(-5)
        };
        
        // Create post for different user
        var otherUserPost = new PostEntity
        {
            Title = "Other User Post",
            Content = "Other content",
            AuthorId = otherAuthorId,
            DateCreated = DateTime.UtcNow
        };
        
        context.Posts.AddRange(userPost1, userPost2, otherUserPost);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByAuthorAsync(userAuthorId);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, post => Assert.Equal(userAuthorId, post.AuthorId));
        
        // Verify ordering (newest first)
        Assert.Equal("User Post 2", resultList[0].Title);
        Assert.Equal("User Post 1", resultList[1].Title);
    }

    [Fact]
    public async Task GetByAuthorAsync_ShouldReturnEmptyList_WhenUserHasNoPosts()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PostRepository(context);
        var userWithNoPosts = "user-with-no-posts";

        // Act
        var result = await repository.GetByAuthorAsync(userWithNoPosts);

        // Assert
        Assert.Empty(result);
    }

    private DataContext CreateContext()
    {
        return new DataContext(new DbContextOptionsBuilder<DataContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options);
    }
}