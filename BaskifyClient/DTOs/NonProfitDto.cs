using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BaskifyClient.DTOs
{
    public class NonProfitDto
    {
        public int EIN { get; set; }
        public string OrganizationName { get; set; }
        public long? Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZIP { get; set; }
        public string Country { get; set; }
        public List<NonProfitMember> Members { get; set; } //a list of people mentioned in 990s
    }

    public class NonProfitMember
    {
        public NonProfitMember()
        {
            CountryCode = 1; //default to USA
        }
        public string Name { get; set; }
        public short CountryCode { get; set; }
        public long PhoneNumber { get; set; }
        public string Position { get; set; }
    }
}
