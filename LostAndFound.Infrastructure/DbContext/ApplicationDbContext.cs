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
        public DbSet<ItemPostMessage> ItemPostMessages => Set<ItemPostMessage>();

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

            builder.Entity<ItemPostMessage>(entity =>
            {
                entity.Property(message => message.Body).HasMaxLength(2000).IsRequired();
                entity.HasIndex(message => message.ConversationId);
                entity.HasOne(message => message.ItemPost).WithMany().HasForeignKey(message => message.ItemPostId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(message => message.SenderUserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(message => message.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
