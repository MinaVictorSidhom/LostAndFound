using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Ui.Models
{
    public class ProfileViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full name")]
        public string PersonName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(30)]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
