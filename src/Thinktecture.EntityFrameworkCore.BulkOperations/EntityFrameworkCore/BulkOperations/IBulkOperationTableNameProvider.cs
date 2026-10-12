using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides the target table names of a bulk operation.
/// The provider is called once for the root entity type and once for every owned entity type that is mapped to a separate table and takes part in the operation, nested owned types included.
/// The provider is called for an owned entity type when the operation reaches that type, so rows of the root table may already be written when the provider throws.
/// Atomicity is the responsibility of the caller's transaction.
/// </summary>
/// <remarks>
/// EF Core creates a distinct <see cref="IEntityType"/> for every ownership, even when two owned navigations use the same CLR type.
/// Distinguish owned entity types by the owning navigation <c>entityType.FindOwnership()?.PrincipalToDependent</c> or by <c>entityType.Name</c>, never by <c>entityType.ClrType</c>.
/// The root entity type has no ownership, i.e. <c>entityType.FindOwnership()</c> returns <c>null</c>.
/// </remarks>
public interface IBulkOperationTableNameProvider
{
   /// <summary>
   /// Resolves the table name and the schema from the EF Core model.
   /// Throws an <see cref="InvalidOperationException"/> for entity types that are configured as temp table entities, because they have no regular table.
   /// </summary>
   public static readonly IBulkOperationTableNameProvider Default = new DefaultBulkOperationTableNameProvider();

   /// <summary>
   /// Creates a provider that writes the root entity to the table <paramref name="tableName"/> in the schema <paramref name="schema"/>.
   /// </summary>
   /// <param name="tableName">Name of the target table of the root entity.</param>
   /// <param name="schema">
   /// Schema of the target table. <c>null</c> means that no schema is emitted, so the database uses its default schema.
   /// It does not mean the schema of the EF Core model.
   /// </param>
   /// <returns>A provider for the root entity type only.</returns>
   /// <exception cref="ArgumentException"><paramref name="tableName"/> is <c>null</c> or white space, or <paramref name="schema"/> is empty or white space.</exception>
   /// <remarks>
   /// The provider throws an <see cref="InvalidOperationException"/> when it is asked for an owned entity type that is mapped to a separate table.
   /// The operation throws when it reaches the owned type. Rows of the root table may already be written at that point.
   /// Atomicity is the responsibility of the caller's transaction.
   /// Use <see cref="Create"/> or implement <see cref="IBulkOperationTableNameProvider"/> to redirect such owned types as well.
   /// </remarks>
   public static IBulkOperationTableNameProvider ForRootEntity(string tableName, string? schema = null)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

      if (schema is not null && String.IsNullOrWhiteSpace(schema))
         throw new ArgumentException("The schema must be null or non-empty.", nameof(schema));

      return new RootEntityTableNameProvider(tableName, schema);
   }

   /// <summary>
   /// Creates a provider that gets the table name of every entity type from <paramref name="getTableName"/>.
   /// The delegate is called for the root entity type and for every owned entity type that is mapped to a separate table.
   /// </summary>
   /// <param name="getTableName">Returns the table name of an entity type.</param>
   /// <returns>A provider that delegates to <paramref name="getTableName"/>.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="getTableName"/> is <c>null</c>.</exception>
   public static IBulkOperationTableNameProvider Create(Func<IEntityType, BulkOperationTableName> getTableName)
   {
      ArgumentNullException.ThrowIfNull(getTableName);

      return new DelegateTableNameProvider(getTableName);
   }

   /// <summary>
   /// Gets the target table name for <paramref name="entityType"/>.
   /// </summary>
   /// <param name="entityType">The root entity type or an owned entity type that is mapped to a separate table.</param>
   /// <returns>
   /// The target table name. A <c>null</c> <see cref="BulkOperationTableName.Schema"/> means that no schema is emitted.
   /// Return <c>entityType.GetSchema()</c> to keep the schema of the model.
   /// </returns>
   BulkOperationTableName GetTableName(IEntityType entityType);
}
