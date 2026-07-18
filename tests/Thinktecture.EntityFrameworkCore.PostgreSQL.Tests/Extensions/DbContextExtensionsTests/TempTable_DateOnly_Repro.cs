using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Parameters;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

/// <summary>
/// Regression tests for issue #75. The temp table entity types TempTable&lt;DateOnly&gt; and
/// TempTable&lt;DateOnly?&gt;, as well as the collection parameter entity types
/// ScalarCollectionParameter&lt;int&gt; and ScalarCollectionParameter&lt;int?&gt;, must keep distinct
/// table names even when another convention renames every entity type with convention source.
/// Without the fix the TempTableConvention and the NpgsqlCollectionParameterConvention configure
/// the table names with convention source, so a later convention-source rename can overwrite them
/// and collapse each pair onto a single shared table name, which makes the model build throw an
/// InvalidOperationException about table sharing. With the fix the table names are configured with
/// data-annotation source, so the convention-source rename is rejected and the distinct names remain.
/// </summary>
public class TempTable_DateOnly_Repro
{
   [Fact]
   public void TempTable_DateOnly_and_nullable_should_keep_distinct_table_names()
   {
      using var context = new MinimalDbContext();
      var model = context.Model;

      var et1 = model.FindEntityType(EntityNameProvider.GetTempTableName(typeof(TempTable<DateOnly>)));
      var et2 = model.FindEntityType(EntityNameProvider.GetTempTableName(typeof(TempTable<DateOnly?>)));

      Assert.NotNull(et1);
      Assert.NotNull(et2);
      Assert.NotEqual(et1.GetTableName(), et2.GetTableName());
   }

   [Fact]
   public void ScalarCollectionParameter_int_and_nullable_should_keep_distinct_table_names()
   {
      using var context = new MinimalDbContext();
      var model = context.Model;

      var et1 = model.FindEntityType(EntityNameProvider.GetCollectionParameterName(typeof(int), true));
      var et2 = model.FindEntityType(EntityNameProvider.GetCollectionParameterName(typeof(int?), true));

      Assert.NotNull(et1);
      Assert.NotNull(et2);
      Assert.NotEqual(et1.GetTableName(), et2.GetTableName());
   }

   private sealed class MinimalDbContext : DbContext
   {
      public DbSet<EntityWithDateOnly> Entities { get; set; } = null!;

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
         optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning,
                                                        CoreEventId.ManyServiceProvidersCreatedWarning));
         optionsBuilder.UseNpgsql("Host=localhost;Database=test;Username=test;Password=test",
                                  o => o.AddBulkOperationSupport()
                                        .AddCollectionParameterSupport());
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);
         modelBuilder.Entity<EntityWithDateOnly>(b =>
         {
            b.HasKey(e => e.Id);
            b.Property(e => e.Date);
            b.Property(e => e.NullableDate);
         });

         // Simulate a naming-convention plugin that renames every entity type with convention
         // source. Without the fix this overwrites the table names set by TempTableConvention and
         // NpgsqlCollectionParameterConvention and collapses each generic-argument pair onto the
         // same table name.
         foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
         {
            ((IConventionEntityType)entityType).Builder.ToTable(entityType.ClrType.Name);
         }
      }
   }

   private sealed class EntityWithDateOnly
   {
      public int Id { get; set; }
      public DateOnly Date { get; set; }
      public DateOnly? NullableDate { get; set; }
   }
}
