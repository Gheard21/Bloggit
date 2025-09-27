using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bloggit.App.Posts.Application.Requests;
using Bloggit.App.Posts.Application.Responses;
using Bloggit.App.Posts.Infrastructure;
using Bloggit.Tests.Posts.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bloggit.Tests.Posts.Api;

[Trait("Category", "Integration")]
public class PostsApiIntegrationTests(PostgresServerFixture fixture) 
    : IClassFixture<PostgresServerFixture>
{
    private readonly PostgresServerFixture _fixture = fixture;

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Configure test environment to disable HTTPS redirection
                builder.UseEnvironment("Testing");
                
                builder.ConfigureServices(services =>
                {
                    // Replace authentication with test authentication
                    services.RemoveAll<IAuthenticationSchemeProvider>();
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", options => { });
                    
                    // Ensure authorization is still configured
                    services.AddAuthorization();
                    
                    // Remove the existing DataContext registration
                    services.RemoveAll<DataContext>();
                    services.RemoveAll<DbContextOptions<DataContext>>();
                    
                    // Add test database context
                    services.AddDbContext<DataContext>(options =>
                        options.UseNpgsql(_fixture.ConnectionString));
                });
            });
    }

    [Fact]
    public async Task GetPost_ShouldReturn200_WhenPostExistsAndUserOwnsIt()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        // Use a post that belongs to the test user
        await using var context = CreateContext();
        var userPost = await context.Posts.FirstAsync(p => p.AuthorId == "test-user-id");

        // Act
        var response = await client.GetAsync($"/api/admin/posts/{userPost.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var postResponse = await response.Content.ReadFromJsonAsync<PostResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(postResponse);
        Assert.Equal(userPost.Id, postResponse.Id);
        Assert.Equal(userPost.Title, postResponse.Title);
        Assert.Equal(userPost.Content, postResponse.Content);
    }

    [Fact]
    public async Task GetPost_ShouldReturn404_WhenPostDoesNotExist()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/admin/posts/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPost_ShouldReturn404_WhenPostExistsButUserDoesNotOwnIt()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        // Use a post that belongs to another user
        await using var context = CreateContext();
        var otherUserPost = await context.Posts.FirstAsync(p => p.AuthorId != "test-user-id");

        // Act
        var response = await client.GetAsync($"/api/admin/posts/{otherUserPost.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllPosts_ShouldReturnOnlyUsersPosts()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/posts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var posts = await response.Content.ReadFromJsonAsync<PostResponse[]>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(posts);
        
        // Should only return posts belonging to test-user-id
        Assert.Equal(2, posts.Length); // We seeded 2 posts for test-user-id
        Assert.All(posts, post => 
        {
            Assert.StartsWith("Test User Post", post.Title);
        });
        
        // Verify ordering (newest first)
        Assert.Equal("Test User Post 2", posts[0].Title);
        Assert.Equal("Test User Post 1", posts[1].Title);
    }

    [Fact]
    public async Task CreatePost_ShouldReturn201_WhenValidRequest()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var request = new NewPostRequest
        {
            Title = "Integration Test Post",
            Content = "This is a test post created during integration testing."
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var postResponse = await response.Content.ReadFromJsonAsync<PostResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(postResponse);
        Assert.Equal(request.Title, postResponse.Title);
        Assert.Equal(request.Content, postResponse.Content);
        Assert.NotEqual(Guid.Empty, postResponse.Id);
        Assert.True(postResponse.CreatedAt > DateTime.MinValue);

        // Verify the Location header
        Assert.True(response.Headers.Location?.ToString().Contains($"/api/admin/posts/{postResponse.Id}"));

        // Verify the post was actually created in the database with correct AuthorId
        await using var context = CreateContext();
        var createdPost = await context.Posts.FindAsync(postResponse.Id);
        Assert.NotNull(createdPost);
        Assert.Equal(request.Title, createdPost.Title);
        Assert.Equal(request.Content, createdPost.Content);
        Assert.Equal("test-user-id", createdPost.AuthorId); // Verify tenant isolation
    }

    [Fact]
    public async Task CreatePost_ShouldReturn400_WhenTitleIsEmpty()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var request = new NewPostRequest
        {
            Title = "", // Empty title should fail validation
            Content = "Valid content"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_ShouldReturn400_WhenContentIsEmpty()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var request = new NewPostRequest
        {
            Title = "Valid Title",
            Content = "" // Empty content should fail validation
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_ShouldReturn400_WhenTitleExceedsMaxLength()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var request = new NewPostRequest
        {
            Title = new string('a', 201), // Exceeds 200 character limit
            Content = "Valid content"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturn200_WhenValidRequestAndUserOwnsPost()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        // Get a post that belongs to the test user
        await using var context = CreateContext();
        var existingPost = await context.Posts.FirstAsync(p => p.AuthorId == "test-user-id");
        
        var request = new UpdatePostRequest
        {
            Id = existingPost.Id,
            Title = "Updated Title",
            Content = "Updated Content"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/admin/posts/{existingPost.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the post was actually updated in the database
        await using var verifyContext = CreateContext();
        var updatedPost = await verifyContext.Posts.FindAsync(existingPost.Id);
        Assert.NotNull(updatedPost);
        Assert.Equal(request.Title, updatedPost.Title);
        Assert.Equal(request.Content, updatedPost.Content);
        Assert.Equal(existingPost.DateCreated, updatedPost.DateCreated); // Should not change
        Assert.Equal(existingPost.AuthorId, updatedPost.AuthorId); // Should not change
    }

    [Fact]
    public async Task UpdatePost_ShouldReturn404_WhenPostExistsButUserDoesNotOwnIt()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        // Get a post that belongs to another user
        await using var context = CreateContext();
        var otherUserPost = await context.Posts.FirstAsync(p => p.AuthorId != "test-user-id");
        
        var request = new UpdatePostRequest
        {
            Id = otherUserPost.Id,
            Title = "Updated Title",
            Content = "Updated Content"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/admin/posts/{otherUserPost.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        // Verify the post was NOT updated in the database
        await using var verifyContext = CreateContext();
        var unchangedPost = await verifyContext.Posts.FindAsync(otherUserPost.Id);
        Assert.NotNull(unchangedPost);
        Assert.Equal(otherUserPost.Title, unchangedPost.Title); // Should remain unchanged
        Assert.Equal(otherUserPost.Content, unchangedPost.Content); // Should remain unchanged
    }

    [Fact]
    public async Task UpdatePost_ShouldReturn404_WhenPostDoesNotExist()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var nonExistentId = Guid.NewGuid();
        
        var request = new UpdatePostRequest
        {
            Id = nonExistentId,
            Title = "Updated Title",
            Content = "Updated Content"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/admin/posts/{nonExistentId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturn400_WhenTitleIsEmpty()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        await using var context = CreateContext();
        var existingPost = await context.Posts.FirstAsync(p => p.AuthorId == "test-user-id");
        
        var request = new UpdatePostRequest
        {
            Id = existingPost.Id,
            Title = "", // Empty title should fail validation
            Content = "Valid content"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/admin/posts/{existingPost.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturn400_WhenContentIsEmpty()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        await using var context = CreateContext();
        var existingPost = await context.Posts.FirstAsync(p => p.AuthorId == "test-user-id");
        
        var request = new UpdatePostRequest
        {
            Id = existingPost.Id,
            Title = "Valid Title",
            Content = "" // Empty content should fail validation
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/admin/posts/{existingPost.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturn400_WhenTitleExceedsMaxLength()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        await using var context = CreateContext();
        var existingPost = await context.Posts.FirstAsync(p => p.AuthorId == "test-user-id");
        
        var request = new UpdatePostRequest
        {
            Id = existingPost.Id,
            Title = new string('a', 201), // Exceeds 200 character limit
            Content = "Valid content"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/admin/posts/{existingPost.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeletePost_ShouldReturn204_WhenPostExistsAndUserOwnsIt()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        // Use a post that belongs to the test user
        await using var context = CreateContext();
        var existingPost = await context.Posts.FirstAsync(p => p.AuthorId == "test-user-id");

        // Act
        var response = await client.DeleteAsync($"/api/admin/posts/{existingPost.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the post was actually deleted from the database
        await using var verifyContext = CreateContext();
        var deletedPost = await verifyContext.Posts.FindAsync(existingPost.Id);
        Assert.Null(deletedPost);
    }

    [Fact]
    public async Task DeletePost_ShouldReturn404_WhenPostExistsButUserDoesNotOwnIt()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        // Use a post that belongs to another user
        await using var context = CreateContext();
        var otherUserPost = await context.Posts.FirstAsync(p => p.AuthorId != "test-user-id");

        // Act
        var response = await client.DeleteAsync($"/api/admin/posts/{otherUserPost.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify the post was NOT deleted from the database
        await using var verifyContext = CreateContext();
        var stillExistsPost = await verifyContext.Posts.FindAsync(otherUserPost.Id);
        Assert.NotNull(stillExistsPost);
    }

    [Fact]
    public async Task DeletePost_ShouldReturn404_WhenPostDoesNotExist()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/admin/posts/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_ShouldPersistCorrectly_WhenMultipleRequestsMade()
    {
        // Arrange
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        
        var requests = new[]
        {
            new NewPostRequest { Title = "Post 1", Content = "Content 1" },
            new NewPostRequest { Title = "Post 2", Content = "Content 2" },
            new NewPostRequest { Title = "Post 3", Content = "Content 3" }
        };

        // Act
        var responses = new List<HttpResponseMessage>();
        var createdPosts = new List<PostResponse>();
        
        foreach (var request in requests)
        {
            var response = await client.PostAsJsonAsync("/api/admin/posts", request);
            responses.Add(response);
            
            if (response.IsSuccessStatusCode)
            {
                var postResponse = await response.Content.ReadFromJsonAsync<PostResponse>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (postResponse != null)
                {
                    createdPosts.Add(postResponse);
                }
            }
        }

        // Assert
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(3, createdPosts.Count);
        
        // Verify all posts were created with unique IDs
        var uniqueIds = createdPosts.Select(p => p.Id).Distinct().ToList();
        Assert.Equal(3, uniqueIds.Count);
        
        // Verify all posts exist in database with correct AuthorId
        await using var context = CreateContext();
        foreach (var createdPost in createdPosts)
        {
            var dbPost = await context.Posts.FindAsync(createdPost.Id);
            Assert.NotNull(dbPost);
            Assert.Equal(createdPost.Title, dbPost.Title);
            Assert.Equal(createdPost.Content, dbPost.Content);
            Assert.Equal("test-user-id", dbPost.AuthorId); // Verify tenant isolation
        }
    }

    private DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        
        return new DataContext(options);
    }
}