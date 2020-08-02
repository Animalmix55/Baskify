using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [StringLength(30)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username must only be alphanumerical")] //any number of alphanumericals
        public string Username { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "The password must be at least 8 characters long.")]
        [RegularExpression(@"(?=^.{8,}$)((?=.*\d)|(?=.*\W+))(?![.\n])(?=.*[A-Z])(?=.*[a-z]).*$",
            ErrorMessage = "Password must contain at least 1 uppercase, 1 lowercase, and a special character")]
        public string Password { get; set; }

        public string redirectUrl { get; set; }
    }
}
