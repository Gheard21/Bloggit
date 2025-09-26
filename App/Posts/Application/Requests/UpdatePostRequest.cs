namespace Bloggit.App.Posts.Application.Requests;

public class UpdatePostRequest
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
}
