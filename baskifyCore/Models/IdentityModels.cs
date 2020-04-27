using System.Data.Entity;
using System.Configuration;


namespace baskifyCore.Models
{
    public class ApplicationDbContext : DbContext
    {
        //public ApplicationDbContext() : base(ConfigurationManager.ConnectionStrings["BasketlyDB"].ConnectionString) { }
        public ApplicationDbContext() : base(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Cory\baskifyCore.Models.ApplicationDbContext.mdf;Integrated Security=True;Connect Timeout=30") { }
        public DbSet<UserModel> UserModel { get; set; }

        public DbSet<BearerTokenModel> BearerTokenModel { get; set; }
        public DbSet<UserAlertModel> UserAlert { get; set; }
        public DbSet<EmailChangeModel> EmailChange { get; set; }

    }
}
