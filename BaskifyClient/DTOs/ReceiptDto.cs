using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.DTOs
{
    public class ReceiptDto
    {
        public string PaymentIntentId { get; set; }
        public DateTime Time { get; set; }
        public string Username { get; set; }
        public UserDto UserModel { get; set; }
        public AuctionDto AuctionModel { get; set; }
        public float Amount { get; set; } //USD IN CENTS
        public string CardholderName { get; set; }
        public string BillingAddress { get; set; }
        public string BillingCity { get; set; }
        public string BillingState { get; set; }
        public string BillingZIP { get; set; }

        public string CardLastFour { get; set; }
        public string CardExp { get; set; }
        public string CardType { get; set; }
    }
}
