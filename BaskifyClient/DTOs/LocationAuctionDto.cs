using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.DTOs
{
    public class LocationAuctionDto
    {
        public int AuctionId { get; set; }
        public string HostUsername { get; set; }
        public OrganizationDto HostUser { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime StartTime { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string BannerImageUrl { get; set; }
        public int MaxRange { get; set; }
        public float TicketCost { get; set; }
        public DateTime? DrawDate { get; set; }
        public bool isLive
        {
            get { return DateTime.UtcNow < EndTime && DateTime.UtcNow >= StartTime; }
        }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZIP { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public int DeliveryType { get; set; }
    }
}
