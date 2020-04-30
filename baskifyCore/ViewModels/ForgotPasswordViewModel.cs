using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.ViewModels
{
    public class ForgotPasswordViewModel : IValidatableObject
    {
        [MaxLength(30, ErrorMessage = "Usernames are no more than 30 characters")]
        public string Username { get; set; }

        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        public bool isEmailValidation { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (String.IsNullOrWhiteSpace(Username) && String.IsNullOrWhiteSpace(Email))
                yield return new ValidationResult("Please input either a username or email", new string[] { "Username", "Email" });
        }
    }
}
