using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

/// <summary>
/// Regression tests for issue #75. The temp table entity types TempTable&lt;DateOnly&gt; and
/// TempTable&lt;DateOnly?&gt;, as well as the collection parameter entity types
/// ScalarCollectionParameter&lt;int&gt; and ScalarCollectionParameter&lt;int?&gt;, keep distinct
/// table names when another convention renames every entity type with convention source.
/// The TempTableConvention and the MySqlCollectionParameterConvention configure the table names
/// with data-annotation source, so a convention-source rename cannot overwrite them. Otherwise each
/// pair would share one table name, and the model build would throw an InvalidOperationException.
/// </summary>
public sealed class PrimitiveTypeTableNames
{
   [Fact]
   public void Should_keep_distinct_table_names_for_DateOnly_and_nullable_DateOnly_temp_tables()
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
   public void Should_keep_distinct_table_names_for_int_and_nullable_int_scalar_collection_parameters()
   {
      using var context = new MinimalDbContext();
      var model = context.Model;

      var et1 = model.FindEntityType(EntityNameProvider.GetCollectionParameterName(typeof(int), true));
      var et2 = model.FindEntityType(EntityNameProvider.GetCollectionParameterName(typeof(int?), true));

      Assert.NotNull(et1);
      Assert.NotNull(et2);
      Assert.NotEqual(et1.GetTableName(), et2.GetTableName());
   }

   [Fact]
   public void Should_keep_distinct_table_names_for_all_temp_tables_of_primitive_types()
   {
      using var context = new MinimalDbContext();

      var tempTableEntityTypes = context.Model.GetEntityTypes()
                                        .Where(entityType => entityType.Name.StartsWith("Thinktecture:TempTable:", StringComparison.Ordinal))
                                        .ToList();

      tempTableEntityTypes.Should().HaveCountGreaterThan(20);
      tempTableEntityTypes.Select(entityType => entityType.GetTableName()).Should().OnlyHaveUniqueItems();
   }

   [Fact]
   public void Should_keep_distinct_table_names_for_all_scalar_collection_parameters()
   {
      using var context = new MinimalDbContext();

      var parameterEntityTypes = context.Model.GetEntityTypes()
                                        .Where(entityType => entityType.Name.StartsWith("Thinktecture:ScalarCollectionParameter:", StringComparison.Ordinal))
                                        .ToList();

      parameterEntityTypes.Should().HaveCountGreaterThan(20);
      parameterEntityTypes.Select(entityType => entityType.GetTableName()).Should().OnlyHaveUniqueItems();
   }

   private sealed class MinimalDbContext : DbContext
   {
      public DbSet<EntityWithDateOnly> Entities { get; set; } = null!;

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
         optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning,
                                                        CoreEventId.ManyServiceProvidersCreatedWarning));
         optionsBuilder.UseMySql("Server=localhost;Database=test",
                                 MySqlContainerFixture.ServerVersion,
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
         // MySqlCollectionParameterConvention and collapses each generic-argument pair onto the
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
