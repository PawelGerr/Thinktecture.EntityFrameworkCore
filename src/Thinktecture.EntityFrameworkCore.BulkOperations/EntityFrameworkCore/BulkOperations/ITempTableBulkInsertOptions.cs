using System;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for bulk insert into a temp table.
/// </summary>
public interface ITempTableBulkInsertOptions
{
   /// <summary>
   /// Options for creation of the temp table.
   /// </summary>
   ITempTableCreationOptions TempTableCreationOptions { get; }

   /// <summary>
   /// Options for bulk insert.
   /// </summary>
   IBulkInsertOptions BulkInsertOptions { get; }
}