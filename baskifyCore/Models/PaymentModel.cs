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

        public bool Locked { get; set; } //INDICATES THAT THE FIELD CANNOT BE EDITED, NOT ENFORECED BY DB
    }
}
