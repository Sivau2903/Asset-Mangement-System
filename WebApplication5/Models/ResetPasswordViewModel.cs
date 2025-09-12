using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string OldPassword { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Confirm Password doesn't match New Password.")]
        public string ConfirmPassword { get; set; }
    }
}