using Microsoft.EntityFrameworkCore;

namespace TinderButForBarteringBackend;

class BarterDatabase : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<User> Users { get; set; }

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