using Bloggit.App.Posts.Domain.Entities;
using Bloggit.App.Posts.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bloggit.App.Posts.Infrastructure.Repositories;

public class PostRepository(DataContext dataContext) : IPostRepository
{
    private readonly DataContext _dataContext = dataContext;

    public async Task<PostEntity> AddAsync(PostEntity post)
    {
        await _dataContext.Posts.AddAsync(post);
        await _dataContext.SaveChangesAsync();
        return post;
    }

    public void Add(PostEntity post)
        => _dataContext.Posts.Add(post);

    public async Task Delete(PostEntity post)
    {
        _dataContext.Posts.Remove(post);
        await SaveChangesAsync();
    }

    public void Remove(PostEntity post)
        => _dataContext.Posts.Remove(post);

    public async Task<PostEntity?> GetByIdAsync(Guid id) =>
        await _dataContext.Posts.FindAsync(id);

    public async Task<PostEntity?> GetByIdAndAuthorAsync(Guid id, string authorId) =>
        await _dataContext.Posts
            .FirstOrDefaultAsync(p => p.Id == id && p.AuthorId == authorId);

    public async Task<IEnumerable<PostEntity>> GetByAuthorAsync(string authorId) =>
        await _dataContext.Posts
            .Where(p => p.AuthorId == authorId)
            .OrderByDescending(p => p.DateCreated)
            .ToListAsync();

    public void Update(PostEntity updatedPost)
        => _dataContext.Posts.Update(updatedPost);
    

    public async Task SaveChangesAsync() =>
        await _dataContext.SaveChangesAsync();
}
