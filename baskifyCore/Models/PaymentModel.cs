using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class PaymentModel
    {
        public PaymentModel()
        {
            Time = DateTime.UtcNow;
        }

        [Required]
        [Key]
        public string PaymentIntentId { get; set; }

        [Required]
        public DateTime Time { get; set; }

        [Required]
        [ForeignKey("UserModel")]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel UserModel { get; set; }

        public bool Complete { get; set; }

        public bool Success { get; set; }

        [ForeignKey("AuctionModel")]
        [Required]
        public int AuctionId { get; set; }

        [ForeignKey("AuctionId")]
        [Required]
        public AuctionModel AuctionModel { get; set; }

        [Required]
        public float Amount { get; set; } //USD IN CENTS


        //Billing address info
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Name can only contain alphabetical letters, hyphens, and spaces")]
        public string CardholderName { get; set; }
        [RegularExpression(@"^[0-9]+(\s[-\.A-Za-z0-9]+)*$", ErrorMessage = "Invalid Address string")]
        public string BillingAddress { get; set; }
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string BillingCity { get; set; }
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string BillingState { get; set; }
        [RegularExpression(@"^([0-9]{5}\-[0-9]{4})|([0-9]{5})$", ErrorMessage = "ZIPs should be in the format ##### or #####-####)")]
        public string BillingZIP { get; set; }
        

        public bool Locked { get; set; } //INDICATES THAT THE FIELD CANNOT BE EDITED, NOT ENFORCED BY DB
    }
}
