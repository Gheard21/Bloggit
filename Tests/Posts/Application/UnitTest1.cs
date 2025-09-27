using Bloggit.App.Posts.Application.Interfaces;
using Bloggit.App.Posts.Application.Mappings;
using Bloggit.App.Posts.Application.Requests;
using Moq;
using Xunit;

namespace Bloggit.Tests.Posts.Application;

public class PostMappingsTests
{
    [Fact]
    public void ToEntity_ShouldCreatePostWithCorrectProperties_WhenUserIsAuthenticated()
    {
        // Arrange
        var mockUserContextService = new Mock<IUserContextService>();
        mockUserContextService.Setup(s => s.GetCurrentUserId()).Returns("test-user-123");
        
        var request = new NewPostRequest
        {
            Title = "Test Post Title",
            Content = "Test post content"
        };

        // Act
        var result = request.ToEntity(mockUserContextService.Object);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Post Title", result.Title);
        Assert.Equal("Test post content", result.Content);
        Assert.Equal("test-user-123", result.AuthorId);
        Assert.True(result.DateCreated > DateTime.MinValue);
        Assert.True(result.DateCreated <= DateTime.UtcNow);
    }

    [Fact]
    public void ToEntity_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var mockUserContextService = new Mock<IUserContextService>();
        mockUserContextService.Setup(s => s.GetCurrentUserId()).Returns((string?)null);
        
        var request = new NewPostRequest
        {
            Title = "Test Post Title",
            Content = "Test post content"
        };

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(() => 
            request.ToEntity(mockUserContextService.Object));
        
        Assert.Equal("User must be authenticated to create posts", exception.Message);
    }

    [Fact]
    public void ToEntity_ShouldThrowUnauthorizedAccessException_WhenUserIdIsEmpty()
    {
        // Arrange
        var mockUserContextService = new Mock<IUserContextService>();
        mockUserContextService.Setup(s => s.GetCurrentUserId()).Returns("");
        
        var request = new NewPostRequest
        {
            Title = "Test Post Title",
            Content = "Test post content"
        };

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(() => 
            request.ToEntity(mockUserContextService.Object));
        
        Assert.Equal("User must be authenticated to create posts", exception.Message);
    }

    [Fact]
    public void ToEntity_ShouldGenerateUniqueIds_WhenCalledMultipleTimes()
    {
        // Arrange
        var mockUserContextService = new Mock<IUserContextService>();
        mockUserContextService.Setup(s => s.GetCurrentUserId()).Returns("test-user-123");
        
        var request1 = new NewPostRequest { Title = "Post 1", Content = "Content 1" };
        var request2 = new NewPostRequest { Title = "Post 2", Content = "Content 2" };

        // Act
        var result1 = request1.ToEntity(mockUserContextService.Object);
        var result2 = request2.ToEntity(mockUserContextService.Object);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
        Assert.NotEqual(Guid.Empty, result1.Id);
        Assert.NotEqual(Guid.Empty, result2.Id);
    }
}
