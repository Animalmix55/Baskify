using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.ViewModels
{
    public class ChangePasswordViewModel : IValidatableObject
    {
        public bool reset { get; set; }

        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "The password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#\$%\^&\*])(?=.{8,})", 
            ErrorMessage = "Password must contain at least 1 uppercase, 1 lowercase, and a special character")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required]
        [Display(Name = "Verify Password")]
        public string VerifyPassword { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var result = new List<ValidationResult>();
            if(NewPassword != VerifyPassword)
            {
                result.Add(new ValidationResult("Passwords Must Match", new string[] { "VerifyPassword" }));
            }
            if (reset & String.IsNullOrEmpty(CurrentPassword))
            {
                result.Add(new ValidationResult("You must provide a current password", new string[] { "CurrentPassword" }));
            }
            return result;
        }
        public void clear()
        {
            CurrentPassword = null;
            NewPassword = null;
            VerifyPassword = null;
        }
    }
}
