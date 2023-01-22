using Microsoft.EntityFrameworkCore;

namespace TinderButForBarteringBackend;

class BarterDatabase : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<DontShowTo> DontShowTo { get; set; } // Products that should not be shown to a user because the user has already swiped on the product or the user is the product's owner
    public DbSet<IsInterested> IsInterested { get; set; } // Entry indicates that the use has swipped 'yes' or 'will pay money' on the product
    public DbSet<WillPay> WillPay { get; set; } // Entry indicates that the use has swipped 'will pay money' on the product

    public string DbPath { get; }


    public BarterDatabase()
    {
        DbPath = Path.Join("data/", "BarterDatabase.db");
    }

    public BarterDatabase(DbContextOptions<BarterDatabase> options) : base(options)
    {
        DbPath = Path.Join("data/", "BarterDatabase.db");
    }
}