using BaskifyClient.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.DTOs
{
    public class SignUpDto : IValidatableObject
    {
        /// <summary>
        /// org or user
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Cannot be empty")]
        public string Type { get; set; }
        /// <summary>
        /// Org ein
        /// </summary>
        public int? EIN { get; set; }
        public List<IFormFile> ProofOfNonProfit { get; set; }
        /// <summary>
        /// 5-digit validation code for orgs
        /// </summary>
        public string ValidationCode { get; set; }

        /// <summary>
        /// Specifies the verification model used
        /// </summary>
        public int? VerifyId { get; set; }

        /// <summary>
        /// Front of id
        /// </summary>
        public IFormFile IdFront { get; set; }
        /// <summary>
        /// opt back of id
        /// </summary>
        public IFormFile IdBack { get; set; }

        /// <summary>
        /// Icon image
        /// </summary>
        public IFormFile Icon { get; set; }

        /// <summary>
        /// Proof of membership in org
        /// </summary>
        public List<IFormFile> MembershipForm { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Cannot be empty")]
        [RegularExpression(@"^[0-9]+(\s[-\.A-Za-z0-9]+)*$", ErrorMessage = "Invalid Address string")]
        public string Address { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Cannot be empty")]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "City is improperly formatted")]
        public string City { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Cannot be empty")]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "State is improperly formatted")]
        public string State { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Cannot be empty")]
        [RegularExpression(@"^([0-9]{5}\-[0-9]{4})|([0-9]{5})$", ErrorMessage = "ZIPs should be in the format ##### or #####-####)")]
        public string ZIP { get; set; }

        [StringLength(30)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username must only be alphanumerical")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Cannot be empty")]
        public string Username { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Cannot be empty")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Cannot be empty")]
        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[#?!@$%^&*-]).{8,}$",
            ErrorMessage = "Password must contain at least 1 uppercase, 1 lowercase, and a special character")]
        public string Password { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Cannot be empty")]
        public string ConfirmPassword { get; set; }

        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Name can only contain alphabetical letters, hyphens, and spaces")]
        public string FirstName { get; set; }

        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Name can only contain alphabetical letters, hyphens, and spaces")]
        public string LastName { get; set; }

        public string MFAPhone { get; set; }

        public string MFASecret { get; set; }
        public int? MFAValId { get; set; }

        /// <summary>
        /// ReCAPTCHA token
        /// </summary>
        public string Token { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Password != ConfirmPassword)
                yield return new ValidationResult("Passwords do not match", new[] { "Password" });
            if(Type == "organization")
            {
                if(EIN == null)
                {
                    if(ProofOfNonProfit == null || ProofOfNonProfit.Count == 0)
                        yield return new ValidationResult("Either select an organization or upload files proving nonprofit status.", new[] { "ProofOfNonProfit" });
                }

                if (string.IsNullOrWhiteSpace(ValidationCode) || ValidationCode.Length != 5) //no verification code means must have uploaded proof
                {
                    if(IdFront == null)
                        yield return new ValidationResult("Without a verification code, ID is required.", new[] { "IdFront" });
                    if(MembershipForm == null)
                        yield return new ValidationResult("Without a verification code, proof of authority is required.", new[] { "MembershipForm" });
                }

                //enforce filetypes
                if (IdFront != null && !IdFront.ContentType.StartsWith("image") && !IdFront.ContentType.Contains("pdf"))
                    yield return new ValidationResult("Invalid Content Type.", new[] { "IdFront" });

                if (IdBack != null && !IdFront.ContentType.StartsWith("image") && !IdFront.ContentType.Contains("pdf"))
                    yield return new ValidationResult("Invalid Content Type.", new[] { "IdBack" });

                if (MembershipForm != null && MembershipForm.Any(m => !m.ContentType.StartsWith("image") && !m.ContentType.Contains("pdf")))
                    yield return new ValidationResult("Invalid Content Type.", new[] { "MembershipForm" });

                if (ProofOfNonProfit != null && ProofOfNonProfit.Any(m => !m.ContentType.StartsWith("image") && !m.ContentType.Contains("pdf")))
                    yield return new ValidationResult("Invalid Content Type.", new[] { "ProofOfNonProfit" });
            }
            else
            {//require first and last names
                if(string.IsNullOrWhiteSpace(FirstName))
                    yield return new ValidationResult("First name is required.", new[] { "FirstName" });
                if (string.IsNullOrWhiteSpace(LastName))
                    yield return new ValidationResult("Last name is required.", new[] { "LastName" });
            }

            if(Icon != null && !Icon.ContentType.StartsWith("image"))
                yield return new ValidationResult("Invalid file type.", new[] { "Icon" });
        }
    }
}
