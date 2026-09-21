using LostAndFound.Core.Domain.IdentityEntities;

namespace LostAndFound.Core.Domain.Posts;

public enum PostType { Lost, Found }
public enum PostStatus { Active, Resolved, Closed }
public enum ContactPreference { InAppMessagesOnly, ShowEmail, ShowPhone }

public class ItemPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public PostType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly ItemDate { get; set; }
    public ContactPreference ContactPreference { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<ItemPostImage> Images { get; set; } = new List<ItemPostImage>();
}

public class ItemPostImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemPostId { get; set; }
    public ItemPost ItemPost { get; set; } = null!;
    public string Path { get; set; } = string.Empty;
}
