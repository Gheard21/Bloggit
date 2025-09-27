using System.Security.Claims;
using Bloggit.App.Posts.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Bloggit.App.Posts.Infrastructure.Services;

/// <summary>
/// Service for accessing the current user's context from HTTP authentication claims.
/// Extracts the NameIdentifier claim to serve as the tenant identifier.
/// </summary>
public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}