using System;
using Bloggit.App.Posts.Application.Requests;
using Bloggit.App.Posts.Application.Responses;
using Bloggit.App.Posts.Domain.Entities;

namespace Bloggit.App.Posts.Application.Mappings;

public static class PostMappings
{
    public static PostEntity ToEntity(this NewPostRequest request)
    {
        return new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            AuthorId = "System", // TODO: Get from authentication context
        };
    }

    public static PostResponse ToResponse(this PostEntity request)
    {
        return new PostResponse
        {
            Id = request.Id,
            Title = request.Title,
            Content = request.Content,
            CreatedAt = request.DateCreated,
        };
    }
}
