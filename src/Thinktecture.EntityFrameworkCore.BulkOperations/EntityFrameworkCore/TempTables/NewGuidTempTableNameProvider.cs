using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Appends a newly generated GUID at the end of the table name to make the name unique.
   /// It is NOT recommended to use this name provider because it bloats the cache of the Entity Framework Core
   /// and the Query Plan Cache of the database.
   /// </summary>
   [Obsolete("This temp table name provider bloats the cache of the Entity Framework Core and the Query Plan Cache of the database. Use 'ReusingTempTableNameProvider' instead.")]
   public class NewGuidTempTableNameProvider : ITempTableNameProvider
   {
      /// <summary>
      /// An instance of <see cref="NewGuidTempTableNameProvider"/>.
      /// </summary>
      public static readonly ITempTableNameProvider Instance = new NewGuidTempTableNameProvider();

      /// <inheritdoc />
      public ITempTableNameLease LeaseName(DbContext ctx, IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         var tableName = entityType.GetTableName();
         tableName = $"{tableName}_{Guid.NewGuid():N}";

         return new TempTableName(tableName);
      }
   }
}
