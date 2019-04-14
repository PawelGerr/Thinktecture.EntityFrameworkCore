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

      public Action<ModelBuilder> ConfigureModel { get; set; }

      public TestDbContext([NotNull] DbContextOptions<TestDbContext> options, [CanBeNull] IDbContextSchema schema)
         : base(options)
      {
         Schema = schema?.Schema;
      }

      protected override void OnModelCreating([NotNull] ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

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
      public IQueryable<InformationSchemaColumn> GetTempTableColumns<TColumn1>()
      {
         return GetTempTableColumns(typeof(TempTable<TColumn1>));
      }

      [NotNull]
      public IQueryable<InformationSchemaColumn> GetTempTableColumns<TColumn1, TColumn2>()
      {
         return GetTempTableColumns(typeof(TempTable<TColumn1, TColumn2>));
      }

      private IQueryable<InformationSchemaColumn> GetTempTableColumns([NotNull] Type type)
      {
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         var tableName = this.GetTableIdentifier(type).TableName;
         var sql = $@"
SELECT
   * 
FROM
   tempdb.INFORMATION_SCHEMA.COLUMNS WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID('tempdb..{tableName}')";

         return Query<InformationSchemaColumn>().FromSql(sql);
      }

      [NotNull]
      public IQueryable<InformationSchemaTableConstraint> GetTempTableConstraints<TColumn1>()
      {
         var tableName = this.GetTableIdentifier(typeof(TempTable<TColumn1>)).TableName;
         var sql = $@"
SELECT
   * 
FROM
   tempdb.INFORMATION_SCHEMA.TABLE_CONSTRAINTS WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID('tempdb..{tableName}')";

         return Query<InformationSchemaTableConstraint>().FromSql(sql);
      }

      [NotNull]
      public IQueryable<InformationSchemaConstraintColumn> GetTempTableConstraintsColumns<TColumn1>()
      {
         var tableName = this.GetTableIdentifier(typeof(TempTable<TColumn1>)).TableName;
         var sql = $@"
SELECT
   * 
FROM
   tempdb.INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID('tempdb..{tableName}')";

         return Query<InformationSchemaConstraintColumn>().FromSql(sql);
      }

      [NotNull]
      public IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns<TColumn1>()
      {
         var tableName = this.GetTableIdentifier(typeof(TempTable<TColumn1>)).TableName;
         var sql = $@"
SELECT
   * 
FROM
   tempdb.INFORMATION_SCHEMA.KEY_COLUMN_USAGE WITH (NOLOCK) 
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID('tempdb..{tableName}')";

         return Query<InformationSchemaKeyColumn>().FromSql(sql);
      }
   }
}
