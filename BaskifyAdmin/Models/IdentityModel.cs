using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyAdmin.Models
{
    public class ApplicationDbContext : DbContext
    {
        //public ApplicationDbContext() : base(ConfigurationManager.ConnectionStrings["BasketlyDB"].ConnectionString) { }
        public ApplicationDbContext() : base("Data Source=tcp:baskifycoredbserver.database.windows.net;Initial Catalog=Baskify;Persist Security Info=True;User ID=animalmix55;Password=@Nimalmix55") { }
        //public ApplicationDbContext() : base(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Cory\baskifyCore.Models.ApplicationDbContext.mdf;Integrated Security=True;Connect Timeout=30") { }
        public DbSet<UserModel> UserModel { get; set; }
        public DbSet<UserAlertModel> UserAlert { get; set; }
        public DbSet<EmailVerificationModel> EmailVerification { get; set; }
        public DbSet<AuctionModel> AuctionModel { get; set; }
        public DbSet<BasketModel> BasketModel { get; set; }
        public DbSet<BasketPhotoModel> BasketPhotoModel { get; set; }
        public DbSet<TicketModel> TicketModel { get; set; }
        public DbSet<UserAuctionWalletModel> UserAuctionWallet { get; set; }
        public DbSet<PendingImageModel> PendingImageModel { get; set; }
        public DbSet<PaymentModel> PaymentModel { get; set; }
        public DbSet<AuctionLinkModel> AuctionLinkModel { get; set; }

        public DbSet<IRSNonProfitDocument> IRSNonProfitDocument { get; set; }
        public DbSet<IRSNonProfit> IRSNonProfit { get; set; }
        public DbSet<TempImageModel> TempImage { get; set; }
        public DbSet<VerificationCodeModel> VerificationCodeModel { get; set; }

        public DbSet<AnonymousClientModel> AnonymousClientModel { get; set; }
        public DbSet<AccountDocumentsModel> AccountDocumentsModel { get; set; }
        
    }
}
