using System;
using System.Reflection;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options for bulk insert into temp tables.
   /// </summary>
   public sealed class SqlServerTempTableBulkInsertOptions : SqlServerTempTableBulkOperationOptions
   {
      /// <summary>
      /// Gets properties to insert.
      /// </summary>
      /// <returns>A collection of <see cref="MemberInfo"/>.</returns>
      public new IEntityPropertiesProvider? PropertiesToInsert
      {
         get => base.PropertiesToInsert;
         set => base.PropertiesToInsert = value;
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerTempTableBulkInsertOptions"/>.
      /// </summary>
      /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
      public SqlServerTempTableBulkInsertOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
         : base(false, optionsToInitializeFrom)
      {
      }
   }
}
