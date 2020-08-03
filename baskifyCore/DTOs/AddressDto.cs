using baskifyCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public class AddressValidationDto
    {
        public string Status { get; set; }
        public string Address {get; set;}
        public string City { get; set; }
        public string County { get; set; }
        public string State { get; set; }
        public string ZIP { get; set; }
        public string Lat { get; set; }
        public string Lng { get; set; }
        public string GoogleMapUrl { get { return !string.IsNullOrWhiteSpace(Address)? accountUtils.getMapLink(Address, City, State, ZIP) : null; } }
    }
}
