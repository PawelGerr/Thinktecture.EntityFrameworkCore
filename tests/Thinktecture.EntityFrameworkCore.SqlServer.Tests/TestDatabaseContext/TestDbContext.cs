using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbContext : DbContext, IDbDefaultSchema
   {
      /// <inheritdoc />
      public string? Schema { get; }

#nullable disable
      public DbSet<TestEntity> TestEntities { get; set; }
      public DbSet<TestEntityWithAutoIncrement> TestEntitiesWithAutoIncrement { get; set; }
      public DbSet<TestEntityWithRowVersion> TestEntitiesWithRowVersion { get; set; }
      public DbSet<TestEntityWithShadowProperties> TestEntitiesWithShadowProperties { get; set; }
#nullable enable

      public Action<ModelBuilder>? ConfigureModel { get; set; }

      public TestDbContext(DbContextOptions<TestDbContext> options, IDbDefaultSchema? schema)
         : base(options)
      {
         Schema = schema?.Schema;
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.Entity<TestEntity>().Property("_privateField");
         modelBuilder.Entity<TestEntity>().Property(e => e.ConvertibleClass).HasConversion(c => c!.Key, k => new ConvertibleClass(k));

         modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).UseIdentityColumn();

         modelBuilder.Entity<TestEntityWithRowVersion>()
                     .Property(e => e.RowVersion)
                     .IsRowVersion()
                     .HasConversion(new NumberToBytesConverter<long>());

         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<string>("ShadowStringProperty").HasMaxLength(50);
         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<int?>("ShadowIntProperty");

         ConfigureModel?.Invoke(modelBuilder);

         modelBuilder.Entity<InformationSchemaColumn>().HasNoKey();
         modelBuilder.Entity<InformationSchemaTableConstraint>().HasNoKey();
         modelBuilder.Entity<InformationSchemaConstraintColumn>().HasNoKey();
         modelBuilder.Entity<InformationSchemaKeyColumn>().HasNoKey();
      }

      public IQueryable<InformationSchemaColumn> GetCustomTempTableColumns<T>()
      {
         return GetTempTableColumns(typeof(T));
      }

      public IQueryable<InformationSchemaColumn> GetTempTableColumns<T>()
         where T : class
      {
         return GetTempTableColumns(typeof(T));
      }

      private IQueryable<InformationSchemaColumn> GetTempTableColumns(Type type)
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

      public IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns<T>()
      {
         var tableName = this.GetEntityType<T>().GetTableName();

         return GetTempTableKeyColumns(tableName);
      }

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
