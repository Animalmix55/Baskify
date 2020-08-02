using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.Models
{
    public enum LockReason
    {
        [Display(Name = "Fradulent Activity")]
        Fraud,
        [Display(Name = "User Requested")]
        Requested,
        [Display(Name = "Email Pending")]
        PendingEmail,
        [Display(Name = "Other")]
        Other
    }
    public static class Roles 
    {
        public const int USER = 1;
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
             CreationDate = DateTime.UtcNow;
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
        [EmailAddress(ErrorMessage = "Invalid Email Format")]
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

        public DateTime CreationDate { get; set; }

        [Required]
        public string iconUrl { get; set; }
        public DateTime lastLogin { get; set; }

        /// <summary>
        /// The user's phone number, with ONLY numerical characters (no parens, spaces, pluses)
        /// </summary>
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Specifies that, in order to login, MFA must be completed
        /// </summary>
        public bool isMFA { get; set; }

        public virtual ICollection<UserAlertModel> UserAlerts { get; set; }

        [ForeignKey("SubmittingUsername")]
        public List<BasketModel> Baskets { get; set; }

        public string StripeCustomerId { get; set; }

        /// <summary>
        /// Holds the hash of the current serialized bearer token, this way we can verify the token was issued by the server
        /// </summary>
        public string BearerHash { get; set; }

        //Indicates if the account is currently locked
        public bool Locked { get; set; }

        //indicates why locked
        public LockReason? LockReason { get; set; }

        /// <summary>
        /// Explains the reason for the lock for OTHER
        /// </summary>
        [MaxLength(100)]
        public string LockDetails { get; set; }

        //---------------------------------------------------------UNMAPPED PROPERTIES FOR APPLICATION WORKFLOW----------------------------------------------------------

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> errormsg = new List<ValidationResult>();
            if (UserRole != Roles.NONE)
            {
                if (String.IsNullOrWhiteSpace(FirstName))
                    errormsg.Add(new ValidationResult("A user must have a first name.", new string[] { "FirstName" }));
                if (String.IsNullOrWhiteSpace(LastName))
                    errormsg.Add(new ValidationResult("A user must have a last name.", new string[] { "LastName" }));
            }

            if(isMFA && string.IsNullOrWhiteSpace(PhoneNumber))
                errormsg.Add(new ValidationResult("To enable MFA, enter a phone number", new string[] { "PhoneNumber" }));

            return errormsg;
        }
    }
}
