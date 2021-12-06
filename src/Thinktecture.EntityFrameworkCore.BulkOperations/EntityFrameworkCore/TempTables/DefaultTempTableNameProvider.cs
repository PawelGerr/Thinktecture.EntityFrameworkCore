using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Use the default name of the entity.
/// </summary>
public class DefaultTempTableNameProvider : ITempTableNameProvider
{
   /// <summary>
   /// An instance of <see cref="DefaultTempTableNameProvider"/>.
   /// </summary>
   public static readonly ITempTableNameProvider Instance = new DefaultTempTableNameProvider();

   /// <inheritdoc />
   public ITempTableNameLease LeaseName(DbContext ctx, IEntityType entityType)
   {
      ArgumentNullException.ThrowIfNull(entityType);

      var tableName = entityType.GetTableName()
                      ?? throw new InvalidOperationException($"The entity '{entityType.Name}' has no table name.");

      return new TempTableName(tableName);
   }
}
