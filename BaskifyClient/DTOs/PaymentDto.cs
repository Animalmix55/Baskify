using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BaskifyClient.DTOs
{
    public class PaymentDto
    {
        [JsonProperty(PropertyName = "id")]
        public string PaymentIntentId { get; set; }
        public DateTime Time { get; set; }
        public string Username { get; set; }
        public bool Complete { get; set; }
        public bool Success { get; set; }
        public AuctionDto AuctionModel { get; set; }
        public float Amount { get; set; } //USD IN CENTS
    }
}
