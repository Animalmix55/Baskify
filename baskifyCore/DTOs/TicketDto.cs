using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public class TicketDto
    {
        public int NumTickets { get; set; }
        public string Username { get; set; }
        public int BasketId { get; set; }
    }
}
