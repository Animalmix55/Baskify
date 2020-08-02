using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.DTOs
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
        public string IconPath
        {
            get
            {
                switch (Card.Brand.ToLower())
                {
                    case CardBrands.AmEx:
                        return "/Content/CardIcons/amex.svg";
                    case CardBrands.DC:
                        return "/Content/CardIcons/diners.svg";
                    case CardBrands.Disc:
                        return "/Content/CardIcons/discover.svg";
                    case CardBrands.JCB:
                        return "/Content/CardIcons/jcb.svg";
                    case CardBrands.MC:
                        return "/Content/CardIcons/mastercard.svg";
                    case CardBrands.UP:
                        return "/Content/CardIcons/unionpay.svg";
                    case CardBrands.Visa:
                        return "/Content/CardIcons/visa.svg";
                    default:
                        return "/Content/CardIcons/generic.svg";
                }
            }
        }
    }

    public static class CardBrands
    {
        public const string AmEx = "amex";
        public const string DC = "diners";
        public const string Disc = "discover";
        public const string JCB = "jcb";
        public const string MC = "mastercard";
        public const string UP = "unionpay";
        public const string Visa = "visa";
        public const string Unknown = "unknown";
    }
}
