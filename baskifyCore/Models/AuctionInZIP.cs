using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class AuctionInZIP
    {
        [Required]
        [Key]
        [Column(Order = 1)]
        [ForeignKey("Auction")]
        public int AuctionId { get; set; }

        [ForeignKey("AuctionId")]
        public AuctionModel Auction { get; set; }

        [RegularExpression("^[0-9]{5}(-[0-9]{4})?$")]
        [Required]
        [Key]
        [Column(Order = 2)]
        public string ZIP { get; set; }

        /// <summary>
        /// Contain city and state if available
        /// </summary>
        public string City { get; set; }

        [ForeignKey("StateModel")]
        public string State { get; set; }

        public StateModel StateModel { get; set; }
    }
}
