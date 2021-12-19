using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture.Database;

public class DemoDbContext : DbContext, IDbDefaultSchema
{
   /// <inheritdoc />
   public string? Schema { get; }

#nullable disable
   public DbSet<Customer> Customers { get; set; }
   public DbSet<Product> Products { get; set; }
   public DbSet<Order> Orders { get; set; }
   public DbSet<OrderItem> OrderItems { get; set; }
#nullable enable

   public DemoDbContext(DbContextOptions<DemoDbContext> options, IDbDefaultSchema? schema = null)
      : base(options)
   {
      Schema = schema?.Schema;
   }

   /// <inheritdoc />
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      modelBuilder.ConfigureTempTable<Guid>();
      modelBuilder.ConfigureTempTable<Guid, Guid>();

      modelBuilder.ConfigureComplexCollectionParameter<MyParameter>();

      modelBuilder.Entity<Customer>(builder =>
                                    {
                                       builder.Property(c => c.FirstName).HasMaxLength(100);
                                       builder.Property(c => c.LastName).HasMaxLength(100);

                                       builder.Property(c => c.RowVersion)
                                              .IsRowVersion()
                                              .HasConversion(new NumberToBytesConverter<long>());
                                    });

      modelBuilder.Entity<OrderItem>().HasKey(i => new { i.OrderId, i.ProductId });
   }
}
