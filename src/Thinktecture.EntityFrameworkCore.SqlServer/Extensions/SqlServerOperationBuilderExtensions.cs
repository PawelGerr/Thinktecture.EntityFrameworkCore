using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Thinktecture.EntityFrameworkCore.Migrations;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="OperationBuilder{TOperation}"/>.
   /// </summary>
   public static class SqlServerOperationBuilderExtensions
   {
      /// <summary>
      /// Annotation key for "IfNotExists".
      /// </summary>
      // ReSharper disable once ConvertToConstant.Global
      public static readonly string IfNotExistsKey = "Thinktecture:OperationBuilderExtensions:IfNotExists";

      /// <summary>
      /// Annotation key for "IfExists".
      /// </summary>
      // ReSharper disable once ConvertToConstant.Global
      public static readonly string IfExistsKey = "Thinktecture:OperationBuilderExtensions:IfExists";

      /// <summary>
      /// Flags the migration that it should be executed if corresponding entity (table, index, etc.) does not exist yet.
      /// </summary>
      /// <remarks>
      /// The <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/> must be used so the annotations have some effect!
      /// Use the extension method "<see cref="SqlServerDbContextOptionsBuilderExtensions.UseThinktectureSqlServerMigrationsSqlGenerator"/>" to change the Migration SQL generator.
      /// </remarks>
      /// <param name="builder">An operation builder.</param>
      /// <typeparam name="T">Type of the migration operation.</typeparam>
      /// <exception cref="ArgumentNullException">Operation builder is <c>null</c>.</exception>
      public static void IfNotExists<T>(this OperationBuilder<T> builder)
         where T : MigrationOperation
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         builder.Annotation(IfNotExistsKey, true);
      }

      /// <summary>
      /// Flags the migration that it should be executed if corresponding entity (table, index, etc.) exists.
      /// </summary>
      /// <remarks>
      /// The <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/> must be used so the annotations have some effect!
      /// Use the extension method "<see cref="SqlServerDbContextOptionsBuilderExtensions.UseThinktectureSqlServerMigrationsSqlGenerator"/>" to change the Migration SQL generator.
      /// </remarks>
      /// <param name="builder">An operation builder.</param>
      /// <typeparam name="T">Type of the migration operation.</typeparam>
      /// <exception cref="ArgumentNullException">Operation builder is <c>null</c>.</exception>
      public static void IfExists<T>(this OperationBuilder<T> builder)
         where T : MigrationOperation
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         builder.Annotation(IfExistsKey, true);
      }

      /// <summary>
      /// Specifies "include columns"
      /// </summary>
      /// <param name="operation">Operation to specify the include columns for.</param>
      /// <param name="columns">Names of the columns.</param>
      /// <returns>The provided <paramref name="operation"/>.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="operation"/> is <c>null</c>.
      /// - or <paramref name="columns"/> collection is <c>null</c>.
      /// </exception>
      /// <exception cref="ArgumentException"><paramref name="columns"/> collection is empty.</exception>
      public static OperationBuilder<CreateIndexOperation> IncludeColumns(this OperationBuilder<CreateIndexOperation> operation, params string[] columns)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));
         if (columns == null)
            throw new ArgumentNullException(nameof(columns));
         if (columns.Length == 0)
            throw new ArgumentException("There must be at least one column in provided collection.", nameof(columns));

         operation.Annotation("SqlServer:Include", columns);

         return operation;
      }

      /// <summary>
      /// Specifies the column as an "identity column".
      /// </summary>
      /// <param name="operation">Operation</param>
      /// <returns>The provided <paramref name="operation"/>.</returns>
      /// <exception cref="ArgumentNullException">The provided <paramref name="operation"/> is <c>null</c>.</exception>
      public static OperationBuilder<AddColumnOperation> AsIdentityColumn(this OperationBuilder<AddColumnOperation> operation)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));

         operation.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

         return operation;
      }

      /// <summary>
      /// Sets the PK as clustered/non-clustered.
      /// </summary>
      /// <param name="operation">Operation</param>
      /// <param name="isClustered">Indication whether the PK should be clustered or not.</param>
      /// <returns>The provided <paramref name="operation"/>.</returns>
      /// <exception cref="ArgumentNullException">The <paramref name="operation"/> is <c>null</c>.</exception>
      public static OperationBuilder<AddPrimaryKeyOperation> IsClustered(this OperationBuilder<AddPrimaryKeyOperation> operation, bool isClustered = true)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));

         operation.Annotation("SqlServer:Clustered", isClustered);

         return operation;
      }
   }
}
