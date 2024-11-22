namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Defines behavior from auto-increment columns.
/// </summary>
public enum SqliteAutoIncrementBehavior
{
   /// <summary>
   /// Sends the value <c>NULL</c> instead of <c>0</c> to the database.
   /// </summary>
   SetZeroToNull,

   /// <summary>
   /// Sends the value as is to the database.
   /// </summary>
   KeepValueAsIs
}