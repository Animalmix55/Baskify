using System.Configuration;
using System.Data.Entity;


namespace baskifyCore.Models
{
    public class ApplicationDbContext : DbContext
    {
        //public ApplicationDbContext() : base(ConfigurationManager.ConnectionStrings["BasketlyDB"].ConnectionString) { }
        public ApplicationDbContext() : base(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Cory\baskifyCore.Models.ApplicationDbContext.mdf;Integrated Security=True;Connect Timeout=30") { }
        public DbSet<UserModel> UserModel { get; set; }

        public DbSet<BearerTokenModel> BearerTokenModel { get; set; }
    }
}
