namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// This is an internal API.
/// </summary>
public sealed class BulkOperationTableNameOptions
{
   private IBulkOperationTableNameProvider? _tableNameProvider;

   /// <summary>
   /// Overrides the table name of the root entity.
   /// Setting this property replaces <see cref="TableNameProvider"/> with a provider for the root entity.
   /// </summary>
   public string? TableName
   {
      get;
      set
      {
         field = value;
         _tableNameProvider = null;
      }
   }

   /// <summary>
   /// Overrides the schema of the root entity.
   /// Setting this property replaces <see cref="TableNameProvider"/> with a provider for the root entity.
   /// </summary>
   public string? Schema
   {
      get;
      set
      {
         field = value;
         _tableNameProvider = null;
      }
   }

   /// <summary>
   /// Provides the target table names.
   /// Setting this property clears <see cref="TableName"/> and <see cref="Schema"/>. The last assignment wins.
   /// Defaults to <see cref="IBulkOperationTableNameProvider.Default"/> when neither the names nor a provider are set.
   /// </summary>
   /// <exception cref="ArgumentNullException">The value to set is <c>null</c>.</exception>
   public IBulkOperationTableNameProvider TableNameProvider
   {
      // A provider that was built from the names is cached until a name changes.
      // When both names are null, 'Default' is cached.
      get => _tableNameProvider ??= ObsoleteRootEntityTableNameProvider.FromOptions(TableName, Schema) ?? IBulkOperationTableNameProvider.Default;
      set
      {
         ArgumentNullException.ThrowIfNull(value);

         TableName = null;
         Schema = null;
         _tableNameProvider = value;
      }
   }

   /// <summary>
   /// Initializes a new instance of <see cref="BulkOperationTableNameOptions"/>.
   /// When <paramref name="tableName"/> or <paramref name="schema"/> is given, the provider is built from them and <paramref name="tableNameProvider"/> is ignored.
   /// </summary>
   /// <param name="tableName">Table name of the root entity.</param>
   /// <param name="schema">Schema of the root entity.</param>
   /// <param name="tableNameProvider">Provider of the target table names.</param>
   public BulkOperationTableNameOptions(string? tableName = null, string? schema = null, IBulkOperationTableNameProvider? tableNameProvider = null)
   {
      TableName = tableName;
      Schema = schema;

      // A provider of the source is built from 'TableName' and 'Schema' when they are set.
      if (tableName is null && schema is null)
         _tableNameProvider = tableNameProvider;
   }
}
