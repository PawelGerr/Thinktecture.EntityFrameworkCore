using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Thinktecture.EntityFrameworkCore.Migrations;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="OperationBuilder{TOperation}"/>.
/// </summary>
public static class NpgsqlOperationBuilderExtensions
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
   /// The <see cref="ThinktectureNpgsqlMigrationsSqlGenerator"/> must be used so the annotations have some effect!
   /// Use the extension method "<see cref="NpgsqlDbContextOptionsBuilderExtensions.UseThinktectureNpgsqlMigrationsSqlGenerator"/>" to change the Migration SQL generator.
   /// </remarks>
   /// <param name="builder">An operation builder.</param>
   /// <typeparam name="T">Type of the migration operation.</typeparam>
   /// <exception cref="ArgumentNullException">Operation builder is <c>null</c>.</exception>
   public static void IfNotExists<T>(this OperationBuilder<T> builder)
      where T : MigrationOperation
   {
      ArgumentNullException.ThrowIfNull(builder);

      builder.Annotation(IfNotExistsKey, true);
   }

   /// <summary>
   /// Flags the migration that it should be executed if corresponding entity (table, index, etc.) exists.
   /// </summary>
   /// <remarks>
   /// The <see cref="ThinktectureNpgsqlMigrationsSqlGenerator"/> must be used so the annotations have some effect!
   /// Use the extension method "<see cref="NpgsqlDbContextOptionsBuilderExtensions.UseThinktectureNpgsqlMigrationsSqlGenerator"/>" to change the Migration SQL generator.
   /// </remarks>
   /// <param name="builder">An operation builder.</param>
   /// <typeparam name="T">Type of the migration operation.</typeparam>
   /// <exception cref="ArgumentNullException">Operation builder is <c>null</c>.</exception>
   public static void IfExists<T>(this OperationBuilder<T> builder)
      where T : MigrationOperation
   {
      ArgumentNullException.ThrowIfNull(builder);

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
      ArgumentNullException.ThrowIfNull(operation);
      ArgumentNullException.ThrowIfNull(columns);

      if (columns.Length == 0)
         throw new ArgumentException("There must be at least one column in provided collection.", nameof(columns));

      operation.Annotation("Npgsql:IndexInclude", columns);

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
      ArgumentNullException.ThrowIfNull(operation);

      operation.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

      return operation;
   }
}
