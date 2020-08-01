using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class AuctionInStateModel
    {
        [ForeignKey("State")]
        [Column(Order = 1)]
        [Key]
        public string StateAbbrv { get; set; }

        [ForeignKey("StateAbbrv")]
        public virtual StateModel State { get; set; }

        [ForeignKey("Auction")]
        [Key]
        [Column(Order = 2)]
        public int AuctionId { get; set; }


        [ForeignKey("AuctionId")]
        public AuctionModel Auction { get; set; }
    }
}
