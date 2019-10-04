using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbContext : DbContext
   {
      public DbSet<TestEntity> TestEntities { get; set; }
      public DbSet<TestEntityWithAutoIncrement> TestEntitiesWithAutoIncrement { get; set; }
      public DbSet<TestEntityWithShadowProperties> TestEntitiesWithShadowProperties { get; set; }

      public Action<ModelBuilder> ConfigureModel { get; set; }

      public TestDbContext([NotNull] DbContextOptions<TestDbContext> options)
         : base(options)
      {
      }

      protected override void OnModelCreating([NotNull] ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.Entity<TestEntity>().Property("_privateField");

         modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).ValueGeneratedOnAdd();

         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<string>("ShadowStringProperty").HasMaxLength(50);
         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<int?>("ShadowIntProperty");

         ConfigureModel?.Invoke(modelBuilder);

         // modelBuilder.Query<InformationSchemaColumn>();
         // modelBuilder.Query<InformationSchemaTableConstraint>();
         // modelBuilder.Query<InformationSchemaConstraintColumn>();
         // modelBuilder.Query<InformationSchemaKeyColumn>();
      }

      //       [JetBrains.Annotations.NotNull]
      //       public IQueryable<InformationSchemaColumn> GetCustomTempTableColumns<T>()
      //       {
      //          return GetTempTableColumns(typeof(T));
      //       }
      //
      //       [JetBrains.Annotations.NotNull]
      //       public IQueryable<InformationSchemaColumn> GetTempTableColumns<T>()
      //          where T : class
      //       {
      //          return GetTempTableColumns(typeof(T));
      //       }
      //
      //       private IQueryable<InformationSchemaColumn> GetTempTableColumns([JetBrains.Annotations.NotNull] Type type)
      //       {
      //          if (type == null)
      //             throw new ArgumentNullException(nameof(type));
      //
      //          var tableName = Model.GetEntityType(type).Relational().TableName;
      //
      //          if (!tableName.StartsWith("#", StringComparison.Ordinal))
      //             tableName = $"#{tableName}";
      //
      //          return Query<InformationSchemaColumn>().FromSql($@"
      // SELECT
      //    *
      // FROM
      //    tempdb.INFORMATION_SCHEMA.COLUMNS WITH (NOLOCK)
      // WHERE
      //    OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
      //       }
      //
      //       [JetBrains.Annotations.NotNull]
      //       public IQueryable<InformationSchemaTableConstraint> GetTempTableConstraints<T>()
      //       {
      //          var tableName = this.GetEntityType<T>().Relational().TableName;
      //
      //          if (!tableName.StartsWith("#", StringComparison.Ordinal))
      //             tableName = $"#{tableName}";
      //
      //          return Query<InformationSchemaTableConstraint>().FromSql($@"
      // SELECT
      //    *
      // FROM
      //    tempdb.INFORMATION_SCHEMA.TABLE_CONSTRAINTS WITH (NOLOCK)
      // WHERE
      //    OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
      //       }
      //
      //       [JetBrains.Annotations.NotNull]
      //       public IQueryable<InformationSchemaConstraintColumn> GetTempTableConstraintsColumns<T>()
      //       {
      //          var tableName = this.GetEntityType<T>().Relational().TableName;
      //
      //          if (!tableName.StartsWith("#", StringComparison.Ordinal))
      //             tableName = $"#{tableName}";
      //
      //          return Query<InformationSchemaConstraintColumn>().FromSql($@"
      // SELECT
      //    *
      // FROM
      //    tempdb.INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE WITH (NOLOCK)
      // WHERE
      //    OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
      //       }
      //
      //       [JetBrains.Annotations.NotNull]
      //       public IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns<T>()
      //       {
      //          var tableName = this.GetEntityType<T>().Relational().TableName;
      //
      //          return GetTempTableKeyColumns(tableName);
      //       }
      //
      //       [JetBrains.Annotations.NotNull]
      //       public IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns<TColumn1, TColumn2>()
      //       {
      //          var tableName = this.GetEntityType<TempTable<TColumn1, TColumn2>>().Relational().TableName;
      //
      //          return GetTempTableKeyColumns(tableName);
      //       }
      //
      //       private IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns(string tableName)
      //       {
      //          if (!tableName.StartsWith("#", StringComparison.Ordinal))
      //             tableName = $"#{tableName}";
      //
      //          return Query<InformationSchemaKeyColumn>().FromSql($@"
      // SELECT
      //    *
      // FROM
      //    tempdb.INFORMATION_SCHEMA.KEY_COLUMN_USAGE WITH (NOLOCK)
      // WHERE
      //    OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
      //       }
   }
}
