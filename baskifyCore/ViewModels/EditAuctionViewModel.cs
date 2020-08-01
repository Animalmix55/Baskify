using baskifyCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.ViewModels
{
    public class EditAuctionViewModel
    {
        public AuctionModel Auction { get; set; }
        public List<StateModel> AllStates { get; set; }
    }
}
