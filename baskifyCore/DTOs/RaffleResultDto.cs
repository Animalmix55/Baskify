using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{

    public static class Results
    {
        public const string PENDING = "Pending";
        public const string LOST = "Lost";
        public const string WON = "Won";
    }
    public class RaffleResultDto
    {
        public BasketDto Basket { get; set; } //DOES NOT INCLUDE WINNER

        public string Status { get; set; }
    }

    public class RaffleDtoGroup
    {
        public LocationAuctionDto auction { get; set; }
        public List<RaffleResultDto> raffleResults { get; set; }
    }
}
