using System.Data.Entity;
using System.Configuration;


namespace baskifyCore.Models
{
    public class ApplicationDbContext : DbContext
    {
        //public ApplicationDbContext() : base(ConfigurationManager.ConnectionStrings["BasketlyDB"].ConnectionString) { }
        public ApplicationDbContext() : base(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Cory\baskifyCore.Models.ApplicationDbContext.mdf;Integrated Security=True;Connect Timeout=30") { }
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
    }
}
