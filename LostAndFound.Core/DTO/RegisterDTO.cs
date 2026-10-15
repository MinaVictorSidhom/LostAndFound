using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LostAndFound.Core.DTO
{
    public class RegisterDTO
    {
        [Required] 
        public string PersonName { get; set; }
        [Required, EmailAddress] 
        public string Email { get; set; }
        [Required, Phone]
        public string PhoneNumber { get; set; }
        [Required, DataType(DataType.Password)] 
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}
