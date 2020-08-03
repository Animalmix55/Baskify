using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class AuctionInCountyModel
    {
        [Key]
        [Column(Order = 2)]
        [ForeignKey("County")]
        public int CountyId { get; set; }

        public virtual CountyModel County { get; set; }

        [ForeignKey("Auction")]
        [Key]
        [Column(Order = 3)]
        public int AuctionId { get; set; }


        [ForeignKey("AuctionId")]
        public AuctionModel Auction { get; set; }
    }
}
