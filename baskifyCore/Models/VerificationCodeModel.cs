using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public enum VerificationType
    {
        EIN,//for making org
        NewPhone,//for anon initial phone num
        LoginMFA,//login
        EnableMFA //enabling or updating MFA
    }
    public class VerificationCodeModel
    {
        /// <summary>
        /// Used by the owner to identify the verification they're sending the code for
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// A value that is checked against when verifying, like EIN or email
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// The verification code that must be matched
        /// </summary>
        [Required]
        public string Secret { get; set; }

        /// <summary>
        /// The time the code was created, if too much time has elapsed, the secret/object is invalid
        /// </summary>
        public DateTime TimeCreated { get; set; }

        /// <summary>
        /// The type of change this object is securing
        /// </summary>
        public VerificationType VerificationType { get; set; }

        /// <summary>
        /// Shows that the use inputted a valid secret and this is now a grant to complete the operation
        /// </summary>
        public bool Validated { get; set; }

        /// <summary>
        /// The time when validated
        /// </summary>
        public DateTime? ValidatedOn { get; set; }

        /// <summary>
        /// A bool that states that this validation is no longer usable
        /// </summary>
        public bool Used { get; set; }

        /// <summary>
        /// For anonymous actions
        /// </summary>
        [ForeignKey("AnonymousClientModel")]
        public Guid? AnonymousClientId { get; set; }


        [ForeignKey("AnonymousClientId")]
        public AnonymousClientModel AnonymousClientModel { get; set; }

        /// <summary>
        /// For logged actions
        /// </summary>
        [ForeignKey("UserModel")]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel UserModel { get; set; }

        public long PhoneNumber { get; set; }
    }
}
