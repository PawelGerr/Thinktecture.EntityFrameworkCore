using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
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
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         var tableName = entityType.GetTableName();

         return new TempTableName(tableName);
      }
   }
}
