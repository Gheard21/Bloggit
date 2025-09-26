using Bloggit.App.Posts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bloggit.App.Posts.Infrastructure;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<PostEntity> Posts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostEntity>(entity =>
        {
            entity.ToTable("Posts");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.AuthorId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.DateCreated)
                .IsRequired();
        });
    }
}
