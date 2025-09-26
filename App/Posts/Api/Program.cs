using Bloggit.App.Posts.Application.Mappings;
using Bloggit.App.Posts.Application.Requests;
using Bloggit.App.Posts.Application.Validators;
using Bloggit.App.Posts.Domain.Interfaces;
using Bloggit.App.Posts.Infrastructure;
using Bloggit.App.Posts.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPostRepository, PostRepository>();

builder.Services.AddScoped<IValidator<NewPostRequest>, NewPostRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePostRequest>, UpdatePostRequestValidator>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

var posts = app.MapGroup("/api/admin/posts");

posts.MapDelete("{postId:guid}", async (Guid postId, IPostRepository postRepository) =>
{
    var post = await postRepository.GetByIdAsync(postId);

    if (post is null)
        return Results.NotFound();

    postRepository.Remove(post);
    await postRepository.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeletePost");

posts.MapGet("{postId:guid}", async (Guid postId, IPostRepository postRepository) =>
{
    var post = await postRepository.GetByIdAsync(postId);

    if (post is null)
        return Results.NotFound();

    var response = post.ToResponse();

    return Results.Ok(response);
})
.WithName("GetPost");

posts.MapPost("", async ([FromBody] NewPostRequest request, IPostRepository postRepository, IValidator<NewPostRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);

    if (!validationResult.IsValid)
        return Results.BadRequest();

    var entity = request.ToEntity();

    await postRepository.AddAsync(entity);
    await postRepository.SaveChangesAsync();

    var response = entity.ToResponse();

    return Results.CreatedAtRoute("GetPost", new { postId = response.Id }, response);
})
.WithName("CreatePost");

posts.MapPatch("{postId:guid}", async (Guid postId, [FromBody] UpdatePostRequest request, IPostRepository postRepository, IValidator<UpdatePostRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);

    if (!validationResult.IsValid)
        return Results.BadRequest();

    var existingPost = await postRepository.GetByIdAsync(postId);

    if (existingPost is null)
        return Results.NotFound();

    existingPost.Title = request.Title;
    existingPost.Content = request.Content;

    postRepository.Update(existingPost);
    await postRepository.SaveChangesAsync();

    return Results.Ok();
})
.WithName("UpdatePost");

app.Run();
