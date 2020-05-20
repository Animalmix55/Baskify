using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class AuctionLinkModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Link { get; set; }

        [ForeignKey("Auction")]
        [Key]
        [Required]
        public int AuctionId { get; set; }

        [ForeignKey("AuctionId")]
        public AuctionModel Auction { get; set; }
    }
}
