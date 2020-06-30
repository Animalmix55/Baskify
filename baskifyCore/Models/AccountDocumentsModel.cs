using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{

    public enum DocumentType
    {
        Identification,
        ProofOfNonProfit,
        ProofOfAuthority
    }
    public class AccountDocumentsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("UserModel")]
        [Required]
        [StringLength(30)]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel UserModel { get; set; }
        public byte[] Document { get; set; }
        public DateTime UploadDate { get; set; }
        public DocumentType Type { get; set; }
        public string ContentType { get; set; }
    }
}
