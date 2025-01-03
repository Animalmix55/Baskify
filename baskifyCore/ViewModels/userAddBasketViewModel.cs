﻿using baskifyCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.ViewModels
{
    public class userAddBasketViewModel
    {
        public AuctionModel Auction { get; set; }
        public List<BasketModel> Baskets { get; set; }
        public Guid AuctionAddLink { get; set; }

        public UserModel User { get; set; }
    }
}
