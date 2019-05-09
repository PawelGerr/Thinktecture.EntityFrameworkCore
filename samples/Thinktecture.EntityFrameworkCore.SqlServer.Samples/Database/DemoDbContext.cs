using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture.Database
{
   public class DemoDbContext : DbContext, IDbContextSchema
   {
      /// <inheritdoc />
      public string Schema { get; }

      public DbSet<Customer> Customers { get; set; }
      public DbSet<Product> Products { get; set; }
      public DbSet<Order> Orders { get; set; }
      public DbSet<OrderItem> OrderItems { get; set; }

      public DemoDbContext([NotNull] DbContextOptions<DemoDbContext> options, [CanBeNull] IDbContextSchema schema = null)
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

         modelBuilder.Entity<OrderItem>().HasKey(i => new { i.OrderId, i.ProductId });
      }
   }
}
