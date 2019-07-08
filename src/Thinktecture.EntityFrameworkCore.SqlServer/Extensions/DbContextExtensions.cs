using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.EntityFrameworkCore.ValueConversion;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class DbContextExtensions
   {
      private static readonly RowVersionValueConverter _rowVersionConverter;

      static DbContextExtensions()
      {
         _rowVersionConverter = new RowVersionValueConverter();
      }

      /// <summary>
      /// Fetches <c>MIN_ACTIVE_ROWVERSION</c> from SQL Server.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>The result of <c>MIN_ACTIVE_ROWVERSION</c> call.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      public static async Task<long> GetMinActiveRowVersionAsync([NotNull] this DbContext ctx, CancellationToken cancellationToken)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         using (var command = ctx.Database.GetDbConnection().CreateCommand())
         {
            command.Transaction = ctx.Database.CurrentTransaction?.GetDbTransaction();

            await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            command.CommandText = "SELECT MIN_ACTIVE_ROWVERSION();";
            var bytes = (byte[])await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return (long)_rowVersionConverter.ConvertFromProvider(bytes);
         }
      }


      /// <summary>
      /// Copies <paramref name="entities"/> into a temp table using <see cref="SqlBulkCopy"/>
      /// and returns the query for accessing the inserted records.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <returns>A query for accessing the inserted values.</returns>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
      [NotNull]
      public static async Task<IQueryable<T>> BulkInsertIntoTempTableAsync<T>([NotNull] this DbContext ctx,
                                                                              [NotNull] IEnumerable<T> entities,
                                                                              [CanBeNull] SqlBulkInsertOptions options = null,
                                                                              CancellationToken cancellationToken = default)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));

         options = options ?? new SqlBulkInsertOptions();
         var tableName = await ctx.CreateTempTableAsync<T>(options.MakeTableNameUnique, cancellationToken).ConfigureAwait(false);

         await BulkInsertAsync(ctx, entities, tableName, options, cancellationToken).ConfigureAwait(false);

         return ctx.GetTempTableQuery<T>(tableName);
      }

      /// <summary>
      /// Copies <paramref name="values"/> into a temp table using <see cref="SqlBulkCopy"/>
      /// and returns the query for accessing the inserted records.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="values">Values to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="TColumn1">Type of the values to insert.</typeparam>
      /// <returns>A query for accessing the inserted values.</returns>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
      [NotNull]
      public static Task<IQueryable<TempTable<TColumn1>>> BulkInsertTempTableAsync<TColumn1>([NotNull] this DbContext ctx,
                                                                                             [NotNull] IEnumerable<TColumn1> values,
                                                                                             [CanBeNull] SqlBulkInsertOptions options = null,
                                                                                             CancellationToken cancellationToken = default)
      {
         if (values == null)
            throw new ArgumentNullException(nameof(values));

         var entities = values.Select(v => new TempTable<TColumn1>(v));

         return ctx.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
      }

      /// <summary>
      /// Copies <paramref name="values"/> into a temp table using <see cref="SqlBulkCopy"/>
      /// and returns the query for accessing the inserted records.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="values">Values to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
      /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
      /// <returns>A query for accessing the inserted values.</returns>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
      [NotNull]
      public static Task<IQueryable<TempTable<TColumn1, TColumn2>>> BulkInsertTempTableAsync<TColumn1, TColumn2>([NotNull] this DbContext ctx,
                                                                                                                 [NotNull] IEnumerable<(TColumn1 column1, TColumn2 column2)> values,
                                                                                                                 [CanBeNull] SqlBulkInsertOptions options = null,
                                                                                                                 CancellationToken cancellationToken = default)
      {
         if (values == null)
            throw new ArgumentNullException(nameof(values));

         var entities = values.Select(t => new TempTable<TColumn1, TColumn2>(t.column1, t.column2));

         return ctx.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
      }

      private static IQueryable<T> GetTempTableQuery<T>([NotNull] this DbContext ctx, [NotNull] string tableName)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

         var entityType = ctx.GetEntityType<T>();
         var sql = $"SELECT * FROM [{tableName}]";

#pragma warning disable EF1000
         if (entityType.IsQueryType)
            return ctx.Query<T>().FromSql(sql);

         return ctx.Set<T>().FromSql(sql);
#pragma warning restore EF1000
      }

      private static async Task BulkInsertAsync<T>([NotNull] this DbContext ctx,
                                                   [NotNull] IEnumerable<T> entities,
                                                   [NotNull] string tableName,
                                                   [NotNull] SqlBulkInsertOptions options,
                                                   CancellationToken cancellationToken)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         var factory = ctx.GetService<IEntityDataReaderFactory>();
         var entityType = ctx.GetEntityType<T>();
         var sqlCon = (SqlConnection)ctx.Database.GetDbConnection();
         var sqlTx = (SqlTransaction)ctx.Database.CurrentTransaction?.GetDbTransaction();

         using (var reader = factory.Create(entities, entityType))
         using (var bulkCopy = new SqlBulkCopy(sqlCon, options.SqlBulkCopyOptions, sqlTx))
         {
            bulkCopy.DestinationTableName = $"[{tableName}]";
            bulkCopy.EnableStreaming = options.EnableStreaming;

            if (options.BulkCopyTimeout.HasValue)
               bulkCopy.BulkCopyTimeout = (int)options.BulkCopyTimeout.Value.TotalSeconds;

            if (options.BatchSize.HasValue)
               bulkCopy.BatchSize = options.BatchSize.Value;

            foreach (var property in entityType.GetProperties())
            {
               var relational = property.Relational();
               var index = reader.GetPropertyIndex(property.PropertyInfo);
               bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(index, relational.ColumnName));
            }

            await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
         }

         if (options.CreatePrimaryKey)
            await ctx.CreatePrimaryKeyAsync<T>(tableName, !options.MakeTableNameUnique, cancellationToken).ConfigureAwait(false);
      }

      private static async Task CreatePrimaryKeyAsync<T>([NotNull] this DbContext ctx, string tableName, bool checkForExistence, CancellationToken cancellationToken)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         var entityType = ctx.GetEntityType<T>();
         IEnumerable<string> columnNames;

         if (entityType.IsQueryType)
         {
            columnNames = entityType.GetProperties().Select(p => p.Relational().ColumnName);
         }
         else
         {
            var pk = entityType.FindPrimaryKey();
            columnNames = pk.Properties.Select(p => p.Relational().ColumnName);
         }

         var sql = $@"
ALTER TABLE [{tableName}]
ADD CONSTRAINT [PK_{tableName}_{Guid.NewGuid():N}] PRIMARY KEY CLUSTERED ({String.Join(", ", columnNames)});
";

         if (checkForExistence)
         {
            sql = $@"
IF(NOT EXISTS (SELECT * FROM tempdb.INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID('tempdb..{tableName}')))
BEGIN
{sql}
END
";
         }
#pragma warning disable EF1000
         await ctx.Database.ExecuteSqlCommandAsync(sql, cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1000
      }
   }
}
