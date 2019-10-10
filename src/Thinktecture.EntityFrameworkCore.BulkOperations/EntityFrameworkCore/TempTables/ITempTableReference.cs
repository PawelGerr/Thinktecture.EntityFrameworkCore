using System;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Represents a reference to temp table that should be disposed if not further required.
   /// </summary>
   public interface ITempTableReference : IDisposable
   {
      /// <summary>
      /// The name of temp table.
      /// </summary>
      string Name { get; }
   }
}