using System.ComponentModel.DataAnnotations;
using LostAndFound.Core.Domain.Posts;
using Microsoft.AspNetCore.Http;

namespace LostAndFound.Ui.Models.Posts;

public class CreateItemPostViewModel
{
    [Required] public PostType Type { get; set; } = PostType.Lost;
    [Required, StringLength(120)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(60)] public string Category { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Location { get; set; } = string.Empty;
    [Required] public DateOnly? ItemDate { get; set; }
    [Required] public ContactPreference ContactPreference { get; set; } = ContactPreference.InAppMessagesOnly;
    public List<IFormFile> Photos { get; set; } = [];
}
