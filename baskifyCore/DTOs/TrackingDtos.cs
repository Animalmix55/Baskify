using baskifyCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public class TrackingDto
    {
        public bool CanCancelDispute { get; set; }
        public bool Editable { get; set; }
        public bool Disputeable { get; set; }
        public bool Disputed { get; set; }
        public string DisputeText { get; set; }
        public string TrackingNumber { get; set; }
        public PostalCarrier Carrier { get; set; }
        public bool Delivered { get; set; }
        public string Destination { get; set; }
        public string Origin { get; set; }
        public List<TrackingItem> Updates { get; set; }

        public DateTime? DeliveryTimeStart { get; set; }
        public DateTime? DeliveryTimeEnd { get; set; }
    }

    public class TrackingItem
    {
        public int id { get; set; }
        public DateTime Time { get; set; }
        public string Location { get; set; }
        public string Message { get; set; }
    }
}
