using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.DTOs
{
    public class IncomingSearchDto
    {
        public class SearchObj
        {
            public string value { get; set; }

            public bool regex { get; set; }
        }

        public class OrderItem
        {
            public string dir { get; set; }

            public int column { get; set; }
        }

        public class ColumnItem
        {
            public string data { get; set; }
            public string name { get; set; }
            public bool searchable { get; set; }
            public bool orderable { get; set; }

            public SearchObj search { get; set; }
        }

        public int draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public SearchObj search { get; set; }

        public List<OrderItem> order { get; set; }

        public List<ColumnItem> columns { get; set; }
    }
}
