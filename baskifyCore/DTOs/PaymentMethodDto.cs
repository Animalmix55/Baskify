using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public class BillingDetailsDto
    {
        public AddressDto Address { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }

    }

    public class AddressDto
    {
        public string City { get; set; }
        public string Country { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string PostalCode { get; set; }
        public string State { get; set; }
    }

    public class CardDto
    {
        public string Brand { get; set; }
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public string Last4 { get; set; }
    }

    public class PaymentMethodDto
    {
        public string Id { get; set; }
        public BillingDetailsDto BillingDetails { get; set; }
        public CardDto Card { get; set; }
        public DateTime Created { get; set; }
    }
}
