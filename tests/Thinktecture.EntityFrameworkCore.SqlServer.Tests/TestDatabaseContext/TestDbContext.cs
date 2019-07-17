using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbContext : DbContext, IDbContextSchema
   {
      /// <inheritdoc />
      public string Schema { get; }

      public DbSet<TestEntity> TestEntities { get; set; }

      public Action<ModelBuilder> ConfigureModel { get; set; }

      public TestDbContext([NotNull] DbContextOptions<TestDbContext> options, [CanBeNull] IDbContextSchema schema)
         : base(options)
      {
         Schema = schema?.Schema;
      }

      protected override void OnModelCreating([NotNull] ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.Entity<TestEntity>().Property("_privateField");
         modelBuilder.Entity<TestEntity>().Property<string>("ShadowProperty").HasMaxLength(50);

         ConfigureModel?.Invoke(modelBuilder);

         modelBuilder.Query<InformationSchemaColumn>();
         modelBuilder.Query<InformationSchemaTableConstraint>();
         modelBuilder.Query<InformationSchemaConstraintColumn>();
         modelBuilder.Query<InformationSchemaKeyColumn>();
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

         var tableName = Model.GetEntityType(type).Relational().TableName;

         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         return Query<InformationSchemaColumn>().FromSql($@"
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
         var tableName = this.GetEntityType<T>().Relational().TableName;

         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         return Query<InformationSchemaTableConstraint>().FromSql($@"
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
         var tableName = this.GetEntityType<T>().Relational().TableName;

         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         return Query<InformationSchemaConstraintColumn>().FromSql($@"
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
         var tableName = this.GetEntityType<T>().Relational().TableName;

         return GetTempTableKeyColumns(tableName);
      }

      [NotNull]
      public IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns<TColumn1, TColumn2>()
      {
         var tableName = this.GetEntityType<TempTable<TColumn1, TColumn2>>().Relational().TableName;

         return GetTempTableKeyColumns(tableName);
      }

      private IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns(string tableName)
      {
         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         return Query<InformationSchemaKeyColumn>().FromSql($@"
SELECT
   * 
FROM
   tempdb.INFORMATION_SCHEMA.KEY_COLUMN_USAGE WITH (NOLOCK) 
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
      }
   }
}
