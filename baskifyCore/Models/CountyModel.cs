using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class CountyModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string CountyName { get; set; }

        [ForeignKey("StateModel")]
        [Required]
        public string StateName { get; set; }

        [ForeignKey("State")]
        public virtual StateModel StateModel { get; set; }

        public bool Equals(string compareCounty, string compareState)
        {
            //removes all non alpha chars, lowercases, and strips the word county, matches state too
            if (compareCounty != null)
                return Regex.Replace(CountyName, @"[^a-zA-Z]", "").ToLower().Replace("county", string.Empty) == Regex.Replace(compareCounty, @"[^a-zA-Z]", "").ToLower().Replace("county", string.Empty) && StateModel.Equals(compareState);
            else
                return StateModel.Equals(compareState);
        }
    }
}
