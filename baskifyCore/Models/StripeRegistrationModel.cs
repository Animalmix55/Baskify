using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class StripeRegistrationModel
    {
        public StripeRegistrationModel()
        {
            TimeCreated = DateTime.UtcNow;
        }

        [Key]
        [Required]
        public Guid State { get; set; }

        [ForeignKey("UserModel")]
        [Required]
        public string Username { get; set; }
        
        [ForeignKey("Username")]
        public UserModel UserModel { get; set; }

        public DateTime TimeCreated { get; set; }

        public bool Complete { get; set; }

        [ForeignKey("EmailVerification")]
        public int? EmailVerificationId { get; set; }

        /// <summary>
        /// Always loaded in
        /// </summary>
        [ForeignKey("EmailVerificationId")]
        public virtual EmailVerificationModel EmailVerification { get; set; }
    }
}
