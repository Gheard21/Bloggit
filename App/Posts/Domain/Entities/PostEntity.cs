namespace Bloggit.App.Posts.Domain.Entities;

public class PostEntity
{
    public Guid Id { get; set; }
    public string AuthorId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
