using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Provides the name of the temp table.
   /// </summary>
   public interface ITempTableNameProvider
   {
      /// <summary>
      /// Gets the name for a temp table of provided <paramref name="entityType"/>.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entityType">Entity type to get the name of temp table for.</param>
      /// <returns>The name of the temp table.</returns>
      string GetName(DbContext ctx, IEntityType entityType);
   }
}
