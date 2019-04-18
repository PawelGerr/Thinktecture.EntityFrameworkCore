using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.Database
{
   public class TestDbContext : DbContext
   {
      public DbSet<Customer> Customers { get; set; }
      public DbSet<Product> Products { get; set; }
      public DbSet<Order> Orders { get; set; }
      public DbSet<OrderItem> OrderItems { get; set; }

      public TestDbContext(DbContextOptions<TestDbContext> options)
         : base(options)
      {
      }

      /// <inheritdoc />
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.ConfigureTempTable<Guid>();
         modelBuilder.ConfigureTempTable<Guid, Guid>();

         modelBuilder.Entity<OrderItem>().HasKey(i => new { i.OrderId, i.ProductId });
      }
   }
}
