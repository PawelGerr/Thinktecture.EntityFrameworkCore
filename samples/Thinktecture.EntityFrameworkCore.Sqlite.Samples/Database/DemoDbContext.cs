using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.Database
{
   public class DemoDbContext : DbContext
   {
#nullable disable
      public DbSet<Customer> Customers { get; set; }
      public DbSet<Product> Products { get; set; }
      public DbSet<Order> Orders { get; set; }
      public DbSet<OrderItem> OrderItems { get; set; }
#nullable enable

      public DemoDbContext(DbContextOptions<DemoDbContext> options)
         : base(options)
      {
      }

      /// <inheritdoc />
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.Entity<OrderItem>().HasKey(i => new { i.OrderId, i.ProductId });
      }
   }
}
