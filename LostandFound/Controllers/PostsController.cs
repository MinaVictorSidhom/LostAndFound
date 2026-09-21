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
        var posts = await _db.ItemPosts.Include(post => post.Images)
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
        var posts = await _db.ItemPosts.Include(post => post.Images).Where(post => post.UserId == user.Id)
            .OrderByDescending(post => post.CreatedAtUtc).ToListAsync();
        return View(posts);
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