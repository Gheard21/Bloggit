namespace Bloggit.App.Posts.Application.Requests;

public class UpdatePostRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
}
