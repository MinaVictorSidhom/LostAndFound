using LostAndFound.Core.Domain.IdentityEntities;
using LostAndFound.Core.Domain.Posts;
using LostAndFound.Infrastructure;
using LostAndFound.Ui.Models.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Ui.Controllers;

public class PostsController : Controller
{
    private const int MaxPhotos = 5;
    private const long MaxPhotoBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedPhotoExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public PostsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
    {
        _db = db;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserName = currentUser?.PersonName;
        }

        var posts = await _db.ItemPosts.Include(post => post.Images).Include(post => post.User)
            .Where(post => post.Status == PostStatus.Active)
            .OrderByDescending(post => post.CreatedAtUtc).ToListAsync();
        return View(posts);
    }

    [Authorize]
    public IActionResult Create() => View(new CreateItemPostViewModel { ItemDate = DateOnly.FromDateTime(DateTime.Today) });

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateItemPostViewModel model)
    {
        ValidatePost(model);
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var post = new ItemPost
        {
            UserId = user.Id,
            Type = model.Type,
            Title = model.Title.Trim(),
            Category = model.Category.Trim(),
            Description = model.Description.Trim(),
            Location = model.Location.Trim(),
            ItemDate = model.ItemDate!.Value,
            ContactPreference = model.ContactPreference
        };
        _db.ItemPosts.Add(post);
        await _db.SaveChangesAsync();
        await SavePhotos(post, model.Photos);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Your report is now active.";
        return RedirectToAction(nameof(MyPosts));
    }

    [Authorize]
    public async Task<IActionResult> MyPosts()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        ViewBag.CurrentUserName = user.PersonName;
        var posts = await _db.ItemPosts.Include(post => post.Images).Where(post => post.UserId == user.Id)
            .OrderByDescending(post => post.CreatedAtUtc).ToListAsync();
        return View(posts);
    }

    [Authorize]
    public async Task<IActionResult> Inbox()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var allMessages = await _db.ItemPostMessages.Include(message => message.ItemPost)
            .Where(message => message.SenderUserId == user.Id || message.RecipientUserId == user.Id)
            .OrderByDescending(message => message.SentAtUtc).ToListAsync();
        var conversations = allMessages.GroupBy(message => message.ConversationId)
            .Select(group => group.First())
            .Select(message => new InboxConversationViewModel
            {
                PostId = message.ItemPostId,
                ConversationId = message.ConversationId,
                ItemTitle = message.ItemPost.Title,
                LatestMessage = message.Body,
                UpdatedAtUtc = message.SentAtUtc
            }).ToList();
        return View(conversations);
    }

    [Authorize]
    public async Task<IActionResult> Contact(Guid id, Guid? conversationId)
    {
        var user = await _userManager.GetUserAsync(User);
        var post = await _db.ItemPosts.SingleOrDefaultAsync(item => item.Id == id && item.Status == PostStatus.Active);
        if (user is null) return Challenge();
        if (post is null) return NotFound();
        if (post.UserId == user.Id && !conversationId.HasValue) return Forbid();

        var model = new ContactItemViewModel { PostId = post.Id, ItemTitle = post.Title, ConversationId = conversationId };
        if (conversationId.HasValue)
        {
            var messages = await _db.ItemPostMessages.Where(message => message.ConversationId == conversationId && message.ItemPostId == post.Id)
                .OrderBy(message => message.SentAtUtc).ToListAsync();
            if (messages.Count == 0 || messages.All(message => message.SenderUserId != user.Id && message.RecipientUserId != user.Id)) return NotFound();
            model.Messages = await ToMessageLines(messages, user.Id);
        }
        return View(model);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactItemViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        var post = await _db.ItemPosts.SingleOrDefaultAsync(item => item.Id == model.PostId && item.Status == PostStatus.Active);
        if (user is null) return Challenge();
        if (post is null) return NotFound();
        if (post.UserId == user.Id && !model.ConversationId.HasValue) return Forbid();
        model.ItemTitle = post.Title;

        Guid recipientId = post.UserId;
        var conversationId = model.ConversationId ?? Guid.NewGuid();
        if (model.ConversationId.HasValue)
        {
            var threadMessages = await _db.ItemPostMessages.Where(message => message.ConversationId == conversationId && message.ItemPostId == post.Id).ToListAsync();
            if (threadMessages.Count == 0 || threadMessages.All(message => message.SenderUserId != user.Id && message.RecipientUserId != user.Id)) return NotFound();
            recipientId = threadMessages[0].SenderUserId == user.Id ? threadMessages[0].RecipientUserId : threadMessages[0].SenderUserId;
        }

        if (!ModelState.IsValid)
        {
            if (model.ConversationId.HasValue)
            {
                var messages = await _db.ItemPostMessages.Where(message => message.ConversationId == conversationId).OrderBy(message => message.SentAtUtc).ToListAsync();
                model.Messages = await ToMessageLines(messages, user.Id);
            }
            return View(model);
        }

        _db.ItemPostMessages.Add(new ItemPostMessage
        {
            ConversationId = conversationId,
            ItemPostId = post.Id,
            SenderUserId = user.Id,
            RecipientUserId = recipientId,
            Body = model.Message.Trim()
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Contact), new { id = post.Id, conversationId });
    }

    [Authorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        var post = await FindOwnedPost(id);
        if (post is null) return NotFound();
        return View(new CreateItemPostViewModel
        {
            Type = post.Type,
            Title = post.Title,
            Category = post.Category,
            Description = post.Description,
            Location = post.Location,
            ItemDate = post.ItemDate,
            ContactPreference = post.ContactPreference
        });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CreateItemPostViewModel model)
    {
        var post = await FindOwnedPost(id);
        if (post is null) return NotFound();
        ValidatePost(model);
        if (!ModelState.IsValid) return View(model);

        post.Type = model.Type; post.Title = model.Title.Trim(); post.Category = model.Category.Trim();
        post.Description = model.Description.Trim(); post.Location = model.Location.Trim();
        post.ItemDate = model.ItemDate!.Value; post.ContactPreference = model.ContactPreference;
        post.UpdatedAtUtc = DateTime.UtcNow;
        await SavePhotos(post, model.Photos);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Your report has been updated.";
        return RedirectToAction(nameof(MyPosts));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id, bool resolved)
    {
        var post = await FindOwnedPost(id);
        if (post is null) return NotFound();
        post.Status = resolved ? PostStatus.Resolved : PostStatus.Closed;
        post.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = resolved ? "Marked as resolved. Great news!" : "Your report has been closed.";
        return RedirectToAction(nameof(MyPosts));
    }

    private void ValidatePost(CreateItemPostViewModel model)
    {
        if (model.ItemDate > DateOnly.FromDateTime(DateTime.Today)) ModelState.AddModelError(nameof(model.ItemDate), "The date cannot be in the future.");
        if (model.Photos.Count > MaxPhotos) ModelState.AddModelError(nameof(model.Photos), $"You can upload up to {MaxPhotos} photos.");
        foreach (var photo in model.Photos.Where(photo => photo.Length > 0))
        {
            if (photo.Length > MaxPhotoBytes) ModelState.AddModelError(nameof(model.Photos), "Each photo must be 5 MB or smaller.");
            if (!AllowedPhotoExtensions.Contains(Path.GetExtension(photo.FileName).ToLowerInvariant())) ModelState.AddModelError(nameof(model.Photos), "Photos must be JPG, PNG, or WebP files.");
        }
    }

    private async Task<ItemPost?> FindOwnedPost(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        return user is null ? null : await _db.ItemPosts.Include(post => post.Images).SingleOrDefaultAsync(post => post.Id == id && post.UserId == user.Id);
    }

    private async Task<List<MessageLineViewModel>> ToMessageLines(List<ItemPostMessage> messages, Guid currentUserId)
    {
        var userIds = messages.SelectMany(message => new[] { message.SenderUserId, message.RecipientUserId }).Distinct();
        var users = await _db.Users.Where(user => userIds.Contains(user.Id)).ToDictionaryAsync(user => user.Id, user => user.PersonName ?? "User");
        return messages.Select(message => new MessageLineViewModel
        {
            SenderName = users.GetValueOrDefault(message.SenderUserId, "User"),
            Body = message.Body,
            SentAtUtc = message.SentAtUtc,
            IsOwnMessage = message.SenderUserId == currentUserId
        }).ToList();
    }

    private async Task SavePhotos(ItemPost post, IEnumerable<IFormFile> photos)
    {
        var actualPhotos = photos.Where(photo => photo.Length > 0).ToList();
        if (actualPhotos.Count == 0) return;
        var root = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var folder = Path.Combine(root, "uploads", "posts", post.Id.ToString());
        Directory.CreateDirectory(folder);
        foreach (var photo in actualPhotos)
        {
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName).ToLowerInvariant()}";
            await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
            await photo.CopyToAsync(stream);

            var image = new ItemPostImage { Path = $"/uploads/posts/{post.Id}/{fileName}" };
            post.Images.Add(image);
            // The image already has a Guid key, so EF Core would assume the row exists and send an UPDATE
            // (0 rows affected -> DbUpdateConcurrencyException). Adding it explicitly makes EF INSERT it.
            _db.Add(image);
        }
    }
}
