using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Ui.Models.Posts;

public class ContactItemViewModel
{
    public Guid PostId { get; set; }
    public Guid? ConversationId { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public List<MessageLineViewModel> Messages { get; set; } = [];

    [Required, StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}

public class MessageLineViewModel
{
    public string SenderName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
    public bool IsOwnMessage { get; set; }
}

public class InboxConversationViewModel
{
    public Guid PostId { get; set; }
    public Guid ConversationId { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public string LatestMessage { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}
