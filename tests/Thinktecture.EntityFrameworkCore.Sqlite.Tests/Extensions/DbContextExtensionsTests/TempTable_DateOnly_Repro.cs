using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

/// <summary>
/// Regression test for issue #75. The temp table entity types TempTable&lt;DateOnly&gt; and
/// TempTable&lt;DateOnly?&gt; must keep distinct table names even when another convention renames
/// every entity type with convention source. Without the fix the TempTableConvention configures
/// the table names with convention source, so a later convention-source rename can overwrite them
/// and collapse both entity types onto the shared table name "TempTable`1", which makes the model
/// build throw an InvalidOperationException about table sharing. With the fix the table names are
/// configured with data-annotation source, so the convention-source rename is rejected and the
/// distinct names remain.
/// </summary>
public class TempTable_DateOnly_Repro
{
   [Fact]
   public void Model_with_DateOnly_temp_tables_should_keep_distinct_table_names()
   {
      var options = new DbContextOptionsBuilder<MinimalDbContext>().Options;
      using var context = new MinimalDbContext(options);

      var model = context.Model;

      var et1 = model.FindEntityType(EntityNameProvider.GetTempTableName(typeof(TempTable<DateOnly>)));
      var et2 = model.FindEntityType(EntityNameProvider.GetTempTableName(typeof(TempTable<DateOnly?>)));

      Assert.NotNull(et1);
      Assert.NotNull(et2);
      Assert.NotEqual(et1.GetTableName(), et2.GetTableName());
   }

   private sealed class MinimalDbContext : DbContext
   {
      public DbSet<EntityWithDateOnly> Entities { get; set; } = null!;

      public MinimalDbContext(DbContextOptions<MinimalDbContext> options)
         : base(options)
      {
      }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
         optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
         optionsBuilder.UseSqlite("Data Source=:memory:", o => o.AddBulkOperationSupport());
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
         // source. Without the fix this overwrites the temp table names set by TempTableConvention
         // and collapses TempTable<DateOnly> and TempTable<DateOnly?> onto the same table name.
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
