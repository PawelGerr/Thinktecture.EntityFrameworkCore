using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Thinktecture.EntityFrameworkCore.Migrations
{
   /// <summary>
   /// Applies the schema to operations.
   /// </summary>
   public interface IMigrationOperationSchemaSetter
   {
      /// <summary>
      /// Applies the schema to <paramref name="operations"/>.
      /// </summary>
      /// <param name="operations">Operations to apply the schema to.</param>
      /// <param name="schema">Database schema.</param>
      void SetSchema([NotNull] IReadOnlyList<MigrationOperation> operations, [CanBeNull] string schema);
   }
}
