using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture;

/// <summary>
/// A wrapper for <see cref="ITableBase"/> to add metadata during translations.
/// </summary>
public class TableWithMetadata : ITableBase
{
   private readonly ITableBase _table;

   /// <summary>
   /// Metadata.
   /// </summary>
   public Dictionary<string, object> Metadata { get; }

   /// <summary>
   /// Initializes new <see cref="TableWithMetadata"/>.
   /// </summary>
   /// <param name="table">Underlying table</param>
   public TableWithMetadata(ITableBase table)
   {
      _table = table;
      Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
   }

   /// <inheritdoc />
   public IAnnotation? FindAnnotation(string name)
   {
      return _table.FindAnnotation(name);
   }

   /// <inheritdoc />
   public IEnumerable<IAnnotation> GetAnnotations()
   {
      return _table.GetAnnotations();
   }

   /// <inheritdoc />
   public object? this[string name] => _table[name];

   /// <inheritdoc />
   public IAnnotation? FindRuntimeAnnotation(string name)
   {
      return _table.FindRuntimeAnnotation(name);
   }

   /// <inheritdoc />
   public IEnumerable<IAnnotation> GetRuntimeAnnotations()
   {
      return _table.GetRuntimeAnnotations();
   }

   /// <inheritdoc />
   public IAnnotation AddRuntimeAnnotation(string name, object? value)
   {
      return _table.AddRuntimeAnnotation(name, value);
   }

   /// <inheritdoc />
   public IAnnotation SetRuntimeAnnotation(string name, object? value)
   {
      return _table.SetRuntimeAnnotation(name, value);
   }

   /// <inheritdoc />
   public IAnnotation? RemoveRuntimeAnnotation(string name)
   {
      return _table.RemoveRuntimeAnnotation(name);
   }

   /// <inheritdoc />
   public TValue GetOrAddRuntimeAnnotationValue<TValue, TArg>(string name, Func<TArg?, TValue> valueFactory, TArg? factoryArgument)
   {
      return _table.GetOrAddRuntimeAnnotationValue(name, valueFactory, factoryArgument);
   }

   /// <inheritdoc />
   public IColumnBase? FindColumn(string name)
   {
      return _table.FindColumn(name);
   }

   /// <inheritdoc />
   public IColumnBase? FindColumn(IProperty property)
   {
      return _table.FindColumn(property);
   }

   /// <inheritdoc />
   public IEnumerable<IForeignKey> GetRowInternalForeignKeys(IEntityType entityType)
   {
      return _table.GetRowInternalForeignKeys(entityType);
   }

   /// <inheritdoc />
   public IEnumerable<IForeignKey> GetReferencingRowInternalForeignKeys(IEntityType entityType)
   {
      return _table.GetReferencingRowInternalForeignKeys(entityType);
   }

   /// <inheritdoc />
   public bool IsOptional(IEntityType entityType)
   {
      return _table.IsOptional(entityType);
   }

   /// <inheritdoc />
   public string Name => _table.Name;

   /// <inheritdoc />
   public string? Schema => _table.Schema;

   /// <inheritdoc />
   public IRelationalModel Model => _table.Model;

   /// <inheritdoc />
   public bool IsShared => _table.IsShared;

   /// <inheritdoc />
   public IEnumerable<ITableMappingBase> EntityTypeMappings => _table.EntityTypeMappings;

   /// <inheritdoc />
   public IEnumerable<IColumnBase> Columns => _table.Columns;
}
