using System.Security.Claims;
using Bloggit.App.Posts.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Bloggit.Tests.Posts.Infrastructure;

public class UserContextServiceTests
{
    [Fact]
    public void GetCurrentUserId_ShouldReturnUserId_WhenUserIsAuthenticated()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        mockHttpContext.Setup(c => c.User).Returns(principal);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
        
        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        Assert.Equal("test-user-123", result);
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);
        
        mockHttpContext.Setup(c => c.User).Returns(principal);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
        
        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnNull_WhenHttpContextIsNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        
        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnNull_WhenUserIsNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        
        mockHttpContext.Setup(c => c.User).Returns((ClaimsPrincipal?)null);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
        
        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnNull_WhenNameIdentifierClaimIsMissing()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com")
            // Missing NameIdentifier claim
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        mockHttpContext.Setup(c => c.User).Returns(principal);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
        
        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnEmptyString_WhenNameIdentifierClaimIsEmpty()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ""), // Empty value
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        mockHttpContext.Setup(c => c.User).Returns(principal);
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
        
        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        Assert.Equal("", result);
    }
}