﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public class OrgBasketDto
    {
        public List<BasketPhotoDto> photos { get; set; }
        public int BasketId { get; set; }
        public UserDto SubmittingUser { get; set; }
        public string BasketTitle { get; set; }
        public string BasketDescription { get; set; }
        public bool AcceptedByOrg { get; set; }
        public List<TicketDto> Tickets { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int AuctionId { get; set; }
        public List<string> BasketContents { get; set; }
        public UserDto Winner { get; set; }
        public AuctionDto AuctionModel { get; set; }
        public int NumTickets
        {
            get { return Tickets.Sum(t => t.NumTickets); }
        }

    }
}
