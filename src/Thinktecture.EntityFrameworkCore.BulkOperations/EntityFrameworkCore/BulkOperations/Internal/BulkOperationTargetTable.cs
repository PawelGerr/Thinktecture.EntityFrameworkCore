using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// The model table of the target entity under the name and schema of the <see cref="IBulkOperationTableNameProvider"/>.
/// A <c>TableExpression</c> takes its name and schema from its <see cref="ITableBase"/>, so the query-based bulk update renders this table as the UPDATE target.
/// </summary>
internal sealed class BulkOperationTargetTable(ITableBase table, BulkOperationTableName tableName) : ITableBase
{
   public string Name => tableName.Name;
   public string? Schema => tableName.Schema;
   public IRelationalModel Model => table.Model;
   public bool IsShared => table.IsShared;
   public IEnumerable<ITableMappingBase> EntityTypeMappings => table.EntityTypeMappings;
   public IEnumerable<ITableMappingBase> ComplexTypeMappings => table.ComplexTypeMappings;
   public IEnumerable<IColumnBase> Columns => table.Columns;
   public IColumnBase? FindColumn(string name) => table.FindColumn(name);
   public IColumnBase? FindColumn(IProperty property) => table.FindColumn(property);
   public IEnumerable<IForeignKey> GetRowInternalForeignKeys(IEntityType entityType) => table.GetRowInternalForeignKeys(entityType);
   public IEnumerable<IForeignKey> GetReferencingRowInternalForeignKeys(IEntityType entityType) => table.GetReferencingRowInternalForeignKeys(entityType);
   public bool IsOptional(ITypeBase typeBase) => table.IsOptional(typeBase);
   public object? this[string name] => table[name];
   public IAnnotation? FindAnnotation(string name) => table.FindAnnotation(name);
   public IEnumerable<IAnnotation> GetAnnotations() => table.GetAnnotations();
   public IAnnotation? FindRuntimeAnnotation(string name) => table.FindRuntimeAnnotation(name);
   public IEnumerable<IAnnotation> GetRuntimeAnnotations() => table.GetRuntimeAnnotations();
   public IAnnotation AddRuntimeAnnotation(string name, object? value) => table.AddRuntimeAnnotation(name, value);
   public IAnnotation SetRuntimeAnnotation(string name, object? value) => table.SetRuntimeAnnotation(name, value);
   public IAnnotation? RemoveRuntimeAnnotation(string name) => table.RemoveRuntimeAnnotation(name);
   public TValue GetOrAddRuntimeAnnotationValue<TValue, TArg>(string name, Func<TArg?, TValue> valueFactory, TArg? factoryArgument) => table.GetOrAddRuntimeAnnotationValue(name, valueFactory, factoryArgument);
   public IColumnBase? FindColumn(IPropertyBase propertyBase) => table.FindColumn(propertyBase);
}
