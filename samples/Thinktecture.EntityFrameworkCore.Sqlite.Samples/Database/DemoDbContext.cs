using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.Database
{
   public class DemoDbContext : DbContext
   {
      public DbSet<Customer> Customers { get; set; }
      public DbSet<Product> Products { get; set; }
      public DbSet<Order> Orders { get; set; }
      public DbSet<OrderItem> OrderItems { get; set; }

      public DemoDbContext([NotNull] DbContextOptions<DemoDbContext> options)
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
