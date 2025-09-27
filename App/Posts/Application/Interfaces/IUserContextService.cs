namespace Bloggit.App.Posts.Application.Interfaces;

/// <summary>
/// Provides access to the current user's context information from authentication claims.
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Gets the current user's unique identifier from the NameIdentifier claim.
    /// This serves as the tenant identifier for multi-tenant scenarios.
    /// </summary>
    /// <returns>The user's unique identifier, or null if no authenticated user.</returns>
    string? GetCurrentUserId();
}