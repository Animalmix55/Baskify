using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class BasketModel
    {
        public List<BasketPhotoModel> photos { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BasketId { get; set; }

        [ForeignKey("SubmittingUser")]
        public string SubmittingUsername { get; set; }

        [ForeignKey("SubmittingUsername")]
        public UserModel SubmittingUser { get; set; }

        //Can get host through auction model

        [Required]
        [RegularExpression(@"^[-A-Za-z0-9]+(\s[-A-Za-z0-9]+)*$", ErrorMessage = "Title can only contain alphabetical letters, hyphens, and spaces")]
        public string BasketTitle { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string BasketDescription { get; set; }

        [Required]
        public bool AcceptedByOrg { get; set; }

        public List<TicketModel> Tickets { get; set; }

        [Required]
        public DateTime SubmissionDate { get; set; }

        [Required]
        [ForeignKey("AuctionModel")]
        public int AuctionId { get; set; }

        [ForeignKey("AuctionId")]
        public AuctionModel AuctionModel {get; set;}

    }
}
