using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{

    /// <summary>
    /// While the ticketModel is the investment a user has in a basket, this is the
    /// investment that a user has in a given auction, particularly unspent tokens
    /// </summary>
    public class UserAuctionWalletModel
    {
        [Key]
        [Column(Order = 1)]
        [ForeignKey("User")]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel User { get; set; }

        [Required]
        public double WalletBalance { get; set; }

        [Key]
        [Column(Order = 2)]
        [ForeignKey("Auction")]
        [Required]
        public int AuctionId { get; set; }

        [ForeignKey("AuctionId")]
        public AuctionModel Auction { get; set; }

        /// <summary>
        /// States whether the user has met the auction's minimum purchase
        /// </summary>
        public bool MinimumMet { get; set; }
    }
}
