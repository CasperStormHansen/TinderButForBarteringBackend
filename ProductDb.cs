using Microsoft.EntityFrameworkCore;

namespace TinderButForBarteringBackend;

class ProductDb : DbContext
{
    public ProductDb(DbContextOptions<ProductDb> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<User> Users => Set<User>();
}
