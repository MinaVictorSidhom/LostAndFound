using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LostAndFound.Core.Domain.IdentityEntities;
using LostAndFound.Core.Domain.Posts;

namespace LostAndFound.Infrastructure
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser,ApplicationRole,Guid>
    {
        public ApplicationDbContext(DbContextOptions options): base(options) 
        {

        }

        public DbSet<ItemPost> ItemPosts => Set<ItemPost>();
        public DbSet<ItemPostImage> ItemPostImages => Set<ItemPostImage>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ItemPost>(entity =>
            {
                entity.Property(post => post.Title).HasMaxLength(120).IsRequired();
                entity.Property(post => post.Category).HasMaxLength(60).IsRequired();
                entity.Property(post => post.Description).HasMaxLength(2000).IsRequired();
                entity.Property(post => post.Location).HasMaxLength(200).IsRequired();
                entity.HasOne(post => post.User).WithMany().HasForeignKey(post => post.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ItemPostImage>(entity =>
            {
                entity.Property(image => image.Path).HasMaxLength(300).IsRequired();
                entity.HasOne(image => image.ItemPost).WithMany(post => post.Images).HasForeignKey(image => image.ItemPostId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
