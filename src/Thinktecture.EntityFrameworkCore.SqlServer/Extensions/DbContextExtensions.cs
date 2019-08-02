using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.BulkOperations;
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
      private static readonly RowVersionValueConverter _rowVersionConverter = new RowVersionValueConverter();

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
            command.CommandText = "SELECT MIN_ACTIVE_ROWVERSION();";

            await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
               var bytes = (byte[])await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

               return (long)_rowVersionConverter.ConvertFromProvider(bytes);
            }
            finally
            {
               ctx.Database.CloseConnection();
            }
         }
      }

      /// <summary>
      /// Creates a temp table using custom type '<typeparamref name="T"/>'.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="makeTableNameUnique">Indication whether the table name should be unique.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of custom temp table.</typeparam>
      /// <returns>Table name</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">The provided type <typeparamref name="T"/> is not known by provided <paramref name="ctx"/>.</exception>
      [NotNull, ItemNotNull]
      public static Task<ITempTableReference> CreateTempTableAsync<T>([NotNull] this DbContext ctx, bool makeTableNameUnique = false, CancellationToken cancellationToken = default)
         where T : class
      {
         return ctx.CreateTempTableAsync(typeof(T), new TempTableCreationOptions { MakeTableNameUnique = makeTableNameUnique }, cancellationToken);
      }

      /// <summary>
      /// Creates a temp table.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="type">Type of the entity.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>Table name</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="ctx"/> is <c>null</c>
      /// - or
      /// <paramref name="type"/> is <c>null</c>.
      /// </exception>
      /// <exception cref="ArgumentException">The provided type <paramref name="type"/> is not known by provided <paramref name="ctx"/>.</exception>
      [NotNull, ItemNotNull]
      public static Task<ITempTableReference> CreateTempTableAsync([NotNull] this DbContext ctx, [NotNull] Type type, [NotNull] TempTableCreationOptions options, CancellationToken cancellationToken)
      {
         var entityType = ctx.Model.GetEntityType(type);
         return ctx.GetService<ITempTableCreator>().CreateTempTableAsync(ctx, entityType, options, cancellationToken);
      }

      /// <summary>
      /// Copies <paramref name="entities"/> into a table using <see cref="SqlBulkCopy"/>.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="propertiesToInsert">Properties to insert.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
      [NotNull]
      public static Task BulkInsertAsync<T>([NotNull] this DbContext ctx,
                                            [NotNull] IEnumerable<T> entities,
                                            [NotNull] Expression<Func<T, object>> propertiesToInsert,
                                            CancellationToken cancellationToken = default)
         where T : class
      {
         var options = new SqlBulkInsertOptions { EntityMembersProvider = EntityMembersProvider.From(propertiesToInsert) };
         return BulkInsertAsync(ctx, entities, options, cancellationToken);
      }

      /// <summary>
      /// Copies <paramref name="entities"/> into a table using <see cref="SqlBulkCopy"/>.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
      [NotNull]
      public static async Task BulkInsertAsync<T>([NotNull] this DbContext ctx,
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
         var entityType = ctx.Model.GetEntityType(typeof(T));

         await ctx.GetService<ISqlServerBulkOperationExecutor>().BulkInsertAsync(ctx, entityType, entities, options, cancellationToken).ConfigureAwait(false);
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
      [NotNull, ItemNotNull]
      public static Task<ITempTableQuery<TempTable<TColumn1>>> BulkInsertValuesIntoTempTableAsync<TColumn1>([NotNull] this DbContext ctx,
                                                                                                           [NotNull] IEnumerable<TColumn1> values,
                                                                                                           [CanBeNull] SqlTempTableBulkInsertOptions options = null,
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
      [NotNull, ItemNotNull]
      public static Task<ITempTableQuery<TempTable<TColumn1, TColumn2>>> BulkInsertValuesIntoTempTableAsync<TColumn1, TColumn2>([NotNull] this DbContext ctx,
                                                                                                                               [NotNull] IEnumerable<(TColumn1 column1, TColumn2 column2)> values,
                                                                                                                               [CanBeNull] SqlTempTableBulkInsertOptions options = null,
                                                                                                                               CancellationToken cancellationToken = default)
      {
         if (values == null)
            throw new ArgumentNullException(nameof(values));

         var entities = values.Select(t => new TempTable<TColumn1, TColumn2>(t.column1, t.column2));

         return ctx.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
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
      [NotNull, ItemNotNull]
      public static async Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>([NotNull] this DbContext ctx,
                                                                                  [NotNull] IEnumerable<T> entities,
                                                                                  [CanBeNull] SqlTempTableBulkInsertOptions options = null,
                                                                                  CancellationToken cancellationToken = default)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));

         options = options ?? new SqlTempTableBulkInsertOptions();
         var entityType = ctx.Model.GetEntityType(typeof(T));
         var tempTableCreator = ctx.GetService<ITempTableCreator>();
         var bulkInsertExecutor = ctx.GetService<ISqlServerBulkOperationExecutor>();

         var tempTableReference = await tempTableCreator.CreateTempTableAsync(ctx, entityType, options.TempTableCreationOptions, cancellationToken).ConfigureAwait(false);

         try
         {
            if (options.PrimaryKeyCreation == PrimaryKeyCreation.BeforeBulkInsert)
               await tempTableCreator.CreatePrimaryKeyAsync(ctx, entityType, tempTableReference.Name, !options.TempTableCreationOptions.MakeTableNameUnique, cancellationToken).ConfigureAwait(false);

            await bulkInsertExecutor.BulkInsertAsync(ctx, entityType, entities, null, tempTableReference.Name, options.BulkInsertOptions, cancellationToken).ConfigureAwait(false);

            if (options.PrimaryKeyCreation == PrimaryKeyCreation.AfterBulkInsert)
               await tempTableCreator.CreatePrimaryKeyAsync(ctx, entityType, tempTableReference.Name, !options.TempTableCreationOptions.MakeTableNameUnique, cancellationToken).ConfigureAwait(false);

            var query = ctx.GetTempTableQuery<T>(entityType, tempTableReference.Name);

            return new TempTableQuery<T>(query, tempTableReference);
         }
         catch (Exception)
         {
            tempTableReference.Dispose();
            throw;
         }
      }

      private static IQueryable<T> GetTempTableQuery<T>([NotNull] this DbContext ctx, [NotNull] IEntityType entityType, [NotNull] string tableName)
         where T : class
      {
         var sql = $"SELECT * FROM [{tableName}]";

#pragma warning disable EF1000
         if (entityType.IsQueryType)
            return ctx.Query<T>().FromSql(sql);

         return ctx.Set<T>().FromSql(sql);
#pragma warning restore EF1000
      }
   }
}
