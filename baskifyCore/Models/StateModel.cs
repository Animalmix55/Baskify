using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class StateModel
    {
        [Required]
        public string Abbrv { get; set; }

        [Key]
        [Required]
        public string FullName { get; set; }

        public List<CountyModel> Counties { get; set; }

        public bool Equals(string compareString)
        {
            return Regex.Replace(FullName, @"\s+", "").ToLower() == Regex.Replace(compareString, @"\s+", "").ToLower() || Regex.Replace(compareString, @"\s+", "").ToLower() == Abbrv.ToLower();
        }
    }
}
