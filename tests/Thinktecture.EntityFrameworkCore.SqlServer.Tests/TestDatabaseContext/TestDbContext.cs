using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.EntityFrameworkCore.ValueConversion;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbContext : DbContext, IDbDefaultSchema
   {
      /// <inheritdoc />
      public string Schema { get; }

      public DbSet<TestEntity> TestEntities { get; set; }
      public DbSet<TestEntityWithAutoIncrement> TestEntitiesWithAutoIncrement { get; set; }
      public DbSet<TestEntityWithRowVersion> TestEntitiesWithRowVersion { get; set; }
      public DbSet<TestEntityWithShadowProperties> TestEntitiesWithShadowProperties { get; set; }

      public Action<ModelBuilder> ConfigureModel { get; set; }

      public TestDbContext([NotNull] DbContextOptions<TestDbContext> options, [CanBeNull] IDbDefaultSchema schema)
         : base(options)
      {
         Schema = schema?.Schema;
      }

      protected override void OnModelCreating([NotNull] ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.Entity<TestEntity>().Property("_privateField");
         modelBuilder.Entity<TestEntity>().Property(e => e.ConvertibleClass).HasConversion(c => c.Key, k => new ConvertibleClass(k));

         modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).UseIdentityColumn();

         modelBuilder.Entity<TestEntityWithRowVersion>()
                     .Property(e => e.RowVersion)
                     .IsRowVersion()
                     .HasConversion(RowVersionValueConverter.Instance);

         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<string>("ShadowStringProperty").HasMaxLength(50);
         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<int?>("ShadowIntProperty");

         ConfigureModel?.Invoke(modelBuilder);

         modelBuilder.Entity<InformationSchemaColumn>().HasNoKey();
         modelBuilder.Entity<InformationSchemaTableConstraint>().HasNoKey();
         modelBuilder.Entity<InformationSchemaConstraintColumn>().HasNoKey();
         modelBuilder.Entity<InformationSchemaKeyColumn>().HasNoKey();
      }

      [NotNull]
      public IQueryable<InformationSchemaColumn> GetCustomTempTableColumns<T>()
      {
         return GetTempTableColumns(typeof(T));
      }

      [NotNull]
      public IQueryable<InformationSchemaColumn> GetTempTableColumns<T>()
         where T : class
      {
         return GetTempTableColumns(typeof(T));
      }

      private IQueryable<InformationSchemaColumn> GetTempTableColumns([NotNull] Type type)
      {
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         var tableName = Model.GetEntityType(type).GetTableName();

         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         return Set<InformationSchemaColumn>().FromSqlInterpolated($@"
SELECT
   *
FROM
   tempdb.INFORMATION_SCHEMA.COLUMNS WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
      }

      [NotNull]
      public IQueryable<InformationSchemaTableConstraint> GetTempTableConstraints<T>()
      {
         var tableName = this.GetEntityType<T>().GetTableName();

         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         return Set<InformationSchemaTableConstraint>().FromSqlInterpolated($@"
SELECT
   *
FROM
   tempdb.INFORMATION_SCHEMA.TABLE_CONSTRAINTS WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
      }

      [NotNull]
      public IQueryable<InformationSchemaConstraintColumn> GetTempTableConstraintsColumns<T>()
      {
         var tableName = this.GetEntityType<T>().GetTableName();

         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         return Set<InformationSchemaConstraintColumn>().FromSqlInterpolated($@"
SELECT
   *
FROM
   tempdb.INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
      }

      [NotNull]
      public IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns<T>()
      {
         var tableName = this.GetEntityType<T>().GetTableName();

         return GetTempTableKeyColumns(tableName);
      }

      [NotNull]
      public IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns<TColumn1, TColumn2>()
      {
         var tableName = this.GetEntityType<TempTable<TColumn1, TColumn2>>().GetTableName();

         return GetTempTableKeyColumns(tableName);
      }

      private IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns(string tableName)
      {
         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         return Set<InformationSchemaKeyColumn>().FromSqlInterpolated($@"
SELECT
   *
FROM
   tempdb.INFORMATION_SCHEMA.KEY_COLUMN_USAGE WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
      }
   }
}
