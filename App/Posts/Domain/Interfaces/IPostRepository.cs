using Bloggit.App.Posts.Domain.Entities;

namespace Bloggit.App.Posts.Domain.Interfaces;

public interface IPostRepository
{
    Task<PostEntity> AddAsync(PostEntity post);
    void Add(PostEntity post);
    Task Delete(PostEntity post);
    void Remove(PostEntity post);
    Task<PostEntity?> GetByIdAsync(Guid id);
    void Update(PostEntity updatedPost);
    Task SaveChangesAsync();
}
