using BaskifyClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.DTOs
{
    public class BasketDto
    {
        public List<BasketPhotoDto> photos { get; set; }

        public int BasketId { get; set; }

        public string BasketTitle { get; set; }

        public string BasketDescription { get; set; }

        public bool AcceptedByOrg { get; set; }

        public DateTime SubmissionDate { get; set; }

        public int AuctionId { get; set; }

        public List<string> BasketContents { get; set; }

        //this has to be populated manually per user
        public int NumTickets { get; set; }

    }
}
