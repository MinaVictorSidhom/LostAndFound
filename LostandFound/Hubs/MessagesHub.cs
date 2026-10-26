using System.Security.Claims;
using LostAndFound.Core.Domain.IdentityEntities;
using LostAndFound.Core.Domain.Posts;
using LostAndFound.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Ui.Hubs;

[Authorize]
public sealed class MessagesHub(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid postId, Guid conversationId)
    {
        if (!TryGetUserId(out var userId)) throw new HubException("Sign in to view messages.");

        var isParticipant = await db.ItemPostMessages.AnyAsync(message =>
            message.ItemPostId == postId && message.ConversationId == conversationId &&
            (message.SenderUserId == userId || message.RecipientUserId == userId));
        if (!isParticipant) throw new HubException("Conversation not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task<Guid> SendMessage(Guid postId, Guid? conversationId, string? body)
    {
        if (!TryGetUserId(out var userId)) throw new HubException("Sign in to send messages.");
        var cleanBody = body?.Trim();
        if (string.IsNullOrWhiteSpace(cleanBody) || cleanBody.Length > 2000)
            throw new HubException("Messages must be between 1 and 2,000 characters.");

        var post = await db.ItemPosts.SingleOrDefaultAsync(item => item.Id == postId && item.Status == PostStatus.Active);
        if (post is null) throw new HubException("This report is no longer active.");

        Guid recipientId;
        var activeConversationId = conversationId ?? Guid.NewGuid();
        if (conversationId.HasValue)
        {
            var thread = await db.ItemPostMessages
                .Where(message => message.ItemPostId == postId && message.ConversationId == activeConversationId)
                .ToListAsync();
            if (thread.Count == 0 || thread.All(message => message.SenderUserId != userId && message.RecipientUserId != userId))
                throw new HubException("Conversation not found.");

            var participantIds = thread.SelectMany(message => new[] { message.SenderUserId, message.RecipientUserId }).Distinct().ToList();
            if (participantIds.Count != 2 || !participantIds.Contains(userId))
                throw new HubException("Conversation not found.");
            recipientId = participantIds.Single(id => id != userId);
        }
        else
        {
            if (post.UserId == userId) throw new HubException("You cannot start a conversation on your own report.");
            recipientId = post.UserId;
        }

        var sender = await userManager.FindByIdAsync(userId.ToString());
        var recipient = await userManager.FindByIdAsync(recipientId.ToString());
        if (sender is null || recipient is null) throw new HubException("A conversation participant could not be found.");

        var message = new ItemPostMessage
        {
            ConversationId = activeConversationId,
            ItemPostId = postId,
            SenderUserId = userId,
            RecipientUserId = recipientId,
            Body = cleanBody,
            SentAtUtc = DateTime.UtcNow
        };
        db.ItemPostMessages.Add(message);
        await db.SaveChangesAsync();

        var messageLine = new LiveMessage(message.Id, activeConversationId, postId, userId,
            sender.PersonName ?? "User", message.Body, message.SentAtUtc);
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(activeConversationId));
        await Clients.Group(ConversationGroup(activeConversationId)).SendAsync("MessageReceived", messageLine);

        var conversationUpdate = new ConversationUpdate(postId, activeConversationId, userId, post.Title, message.Body, message.SentAtUtc);
        await Clients.Group(UserGroup(userId)).SendAsync("ConversationUpdated", conversationUpdate);
        await Clients.Group(UserGroup(recipientId)).SendAsync("ConversationUpdated", conversationUpdate);
        return activeConversationId;
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static string UserGroup(Guid userId) => $"user:{userId}";
    private static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId}";

    public sealed record LiveMessage(Guid Id, Guid ConversationId, Guid PostId, Guid SenderUserId,
        string SenderName, string Body, DateTime SentAtUtc);

    public sealed record ConversationUpdate(Guid PostId, Guid ConversationId, Guid SenderUserId, string ItemTitle,
        string LatestMessage, DateTime UpdatedAtUtc);
}
