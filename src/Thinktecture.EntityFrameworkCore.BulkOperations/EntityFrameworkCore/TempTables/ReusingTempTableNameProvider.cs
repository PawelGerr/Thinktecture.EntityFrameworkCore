using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.TempTables.NameSuffixing;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Re-uses the temp table names.
/// </summary>
public class ReusingTempTableNameProvider : ITempTableNameProvider
{
   /// <summary>
   /// An instance of <see cref="ReusingTempTableNameProvider"/>.
   /// </summary>
   public static readonly ITempTableNameProvider Instance = new ReusingTempTableNameProvider();

   /// <inheritdoc />
   public ITempTableNameLease LeaseName(DbContext ctx, IEntityType entityType)
   {
      ArgumentNullException.ThrowIfNull(ctx);
      ArgumentNullException.ThrowIfNull(entityType);

      var nameLeasing = ctx.GetService<TempTableSuffixLeasing>();
      var suffixLease = nameLeasing.Lease(entityType);

      try
      {
         var tableName = entityType.GetTableName();
         tableName = $"{tableName}_{suffixLease.Suffix}";

         return new TempTableNameLease(tableName, suffixLease);
      }
      catch
      {
         suffixLease.Dispose();
         throw;
      }
   }

   private class TempTableNameLease : ITempTableNameLease
   {
      private TempTableSuffixLease _suffixLease;

      public string Name { get; }

      public TempTableNameLease(string name, TempTableSuffixLease suffixLease)
      {
         Name = name ?? throw new ArgumentNullException(nameof(name));
         _suffixLease = suffixLease;
      }

      public void Dispose()
      {
         _suffixLease.Dispose();
         _suffixLease = default;
      }
   }
}
