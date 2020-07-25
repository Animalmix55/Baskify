using System.Data.Entity;
using System.Configuration;
using System.Data.Entity.Migrations;
using Stripe;

namespace baskifyCore.Models
{
    public class ApplicationDbContext : DbContext
    {
#if !DEBUG
        //public ApplicationDbContext() : base(ConfigurationManager.ConnectionStrings["BasketlyDBProd"].ConnectionString) { }
        public ApplicationDbContext() : base("Data Source=baskifycoredbserver.database.windows.net;Initial Catalog=Baskify_PROD;User ID=animalmix55;Password=@Nimalmix55") { }
#endif
#if DEBUG
        //public ApplicationDbContext() : base(ConfigurationManager.ConnectionStrings["BasketlyDBTest"].ConnectionString) { }
        public ApplicationDbContext() : base("Data Source=tcp:baskifycoredbserver.database.windows.net;Initial Catalog=Baskify;Persist Security Info=True;User ID=animalmix55;Password=@Nimalmix55") { }
#endif

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
        public DbSet<StripeRegistrationModel> StripeRegistrationModel { get; set; }
    }
}
