using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public static class ChangeTypes
    {
        public const int EMAIL = 1;
        public const int AUCTIONDELETION = 2;
    }

    public class EmailVerificationModel : IValidatableObject
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        [Key]
        public int ChangeId { get; set; }
        
        [Required]
        public int ChangeType { get; set; }

        [Required]
        [ForeignKey("UserModel")]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel UserModel { get; set; }

        [Required]
        public string Payload { get; set; }

        [Required]
        public DateTime ChangeTime { get; set; }

        //The revert and committ IDs allow for reversing and committing of email changes via change URLs.
        public Guid RevertId { get; set; }

        [Required]
        public bool CanRevert { get; set; }

        [Required]
        public Guid CommitId { get; set; }

        [Required]
        public bool Committed { get; set; }

        [ForeignKey("UserAlertModel")]
        public Guid? AlertId { get; set; }

        [ForeignKey("AlertId")]
        public UserAlertModel UserAlertModel { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            switch (ChangeType)
            {
                case ChangeTypes.EMAIL:
                    var regex = new Regex(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
                    if (!regex.IsMatch(Payload))
                        yield return new ValidationResult("INVALID EMAIL FORMAT", new[] { "NewValue" });

                    /*
                    if (AlertId == Guid.Empty || AlertId == null)
                        yield return new ValidationResult("EMAIL ALERTS MUST BE TIED TO EMAIL CHANGES", new[] { "AlertId" }); //require email alert
                    */
                    break;
                    //this is expandable should more email verification changes be needed!
            }
            if (CanRevert && RevertId == Guid.Empty)
                yield return new ValidationResult("MUST PROVIDE A REVERTID", new[] { "RevertId" }); //require a revert ID
        }
    }
}
