using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public static class Roles 
    {
        public const int USER = 1;
        public const int COMPANY = 2;
        public const int NONE = 0;
        
    }
    public class UserModel : IValidatableObject
    {
        public UserModel() 
        {
            //defaults for empty user
             FirstName = String.Empty;
             LastName = String.Empty;
             UserRole = Roles.NONE;
             iconUrl = "/Content/unknownUser.png";
        }

        [StringLength(30)]
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage="Username must only be alphanumerical")] //any number of alphanumericals
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        [Display(Name = "First Name")]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Name can only contain alphabetical letters, hyphens, and spaces")]
        public string FirstName
        { get; set; }

        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Display(Name = "Last Name")]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Name can only contain alphabetical letters, hyphens, and spaces")]
        public string LastName
        { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]+(\s[-\.A-Za-z0-9]+)*$", ErrorMessage = "Invalid Address string")]
        public string Address { get; set; }

        [Required]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string City { get; set; }

        [Required]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string State { get; set; }

        [Required]
        [RegularExpression(@"^([0-9]{5}\-[0-9]{4})|([0-9]{5})$", ErrorMessage = "ZIPs should be in the format ##### or #####-####)")]
        public string ZIP { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        [Required]
        public int UserRole
        { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string iconUrl { get; set; }
        public DateTime lastLogin { get; set; }

        public ICollection<UserAlertModel> UserAlerts { get; set; }

        [ForeignKey("SubmittingUsername")]
        public List<BasketModel> Baskets { get; set; }

        public string StripeCustomerId { get; set; }

        /// <summary>
        /// Holds the hash of the current serialized bearer token, this way we can verify the token was issued by the server
        /// </summary>
        public string BearerHash { get; set; }

        //---------------------------------------------------------ORGANIZATION-SPECIFIC PROPERTIES----------------------------------------------------------------------

        [Display(Name = "Organization Name")]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Name can only contain alphabetical letters, hyphens, and spaces")]
        public string OrganizationName { get; set; }

        public bool OrganizationVerified { get; set; }

        public ICollection<AuctionModel> Auctions { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address!")]
        [Display(Name = "Public Contact Email")]
        public string ContactEmail { get; set; }

        //---------------------------------------------------------UNMAPPED PROPERTIES FOR APPLICATION WORKFLOW----------------------------------------------------------
        [NotMapped]
        public string bearerToken { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> errormsg = new List<ValidationResult>();
            if (UserRole != Roles.COMPANY)
            {
                if (String.IsNullOrWhiteSpace(FirstName))
                    errormsg.Add(new ValidationResult("A user must have a first name.", new string[] { "FirstName" }));
                if (String.IsNullOrWhiteSpace(LastName))
                    errormsg.Add(new ValidationResult("A user must have a last name.", new string[] { "LastName" }));
                if (DateOfBirth == null)
                    errormsg.Add(new ValidationResult("A user must have a birthdate.", new String[] { "DateOfBirth" }));
            }
            else if (UserRole == Roles.COMPANY)
            {
                if (String.IsNullOrWhiteSpace(OrganizationName))
                    errormsg.Add(new ValidationResult("Organization Name Must Not Be Empty", new string[] { "OrganizationName" }));
                if (String.IsNullOrEmpty(ContactEmail))
                    errormsg.Add(new ValidationResult("Invalid Contact Email", new string[] { "ContactEmail" }));
            }
            return errormsg;
        }
    }
}
