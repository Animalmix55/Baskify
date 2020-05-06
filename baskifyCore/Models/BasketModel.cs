﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;


namespace baskifyCore.Models
{
    public class BasketModel : IValidatableObject
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
        [Display(Name = "Basket Title")]
        public string BasketTitle { get; set; }

        [Display(Name = "Basket Description")]
        [Required(AllowEmptyStrings = false)]
        public string BasketDescription { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Basket Content is required!")]
        public string BasketContentString
        {
            get { return (BasketContents == null || BasketContents.Count == 0)? null : JsonSerializer.Serialize(BasketContents); }
            set { BasketContents = (value == null)? new List<string>() : JsonSerializer.Deserialize<List<string>>(value); }
        }

        public bool AcceptedByOrg { get; set; }

        public List<TicketModel> Tickets { get; set; }

        public DateTime SubmissionDate { get; set; }

        [Required]
        [ForeignKey("AuctionModel")]
        public int AuctionId { get; set; }

        [ForeignKey("AuctionId")]
        public AuctionModel AuctionModel {get; set;}

        //--------------------------------------------UNMAPPED ATTRIBUTES---------------------------------

        [NotMapped]
        public List<string> addImages { get; set; }
        public List<string> removeImages { get; set; }

        [Required]
        [Display(Name = "Basket Contents")]
        public List<string> BasketContents { get; set; }
            /*
        {
            get { return string.IsNullOrWhiteSpace(BasketContentString) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(BasketContentString); }
            set { BasketContentString = (value == null || value.Count == 0)? null : JsonSerializer.Serialize(value); }
        }
        */

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BasketContents.Any(b => string.IsNullOrWhiteSpace(b)))
                yield return new ValidationResult("No content rows can be empty!", new[] { "BasketContentString" }); //no empty indeces allowed
        }
    }
}
