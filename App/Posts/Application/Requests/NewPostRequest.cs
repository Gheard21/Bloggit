namespace Bloggit.App.Posts.Application.Requests;

public class NewPostRequest
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
}
