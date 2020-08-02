using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BaskifyClient.Models
{
    /// <summary>
    /// Represents a SINGLE photo attached to a basket
    /// </summary>
    public class BasketPhotoModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PhotoId { get; set; }

        [Required]
        public string Url { get; set; }

        [Display(Name = "Photo Description")]
        [RegularExpression(@"[A-Za-z0-9\s]*")]
        public string PhotoDesc { get; set; }

        [Required]
        [ForeignKey("Basket")]
        public int BasketId { get; set; }

        [ForeignKey("BasketId")]
        public BasketModel Basket { get; set; }
    }
}
