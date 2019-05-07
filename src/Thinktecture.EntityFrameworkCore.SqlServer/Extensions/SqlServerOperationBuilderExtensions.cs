using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="OperationBuilder{TOperation}"/>.
   /// </summary>
   public static class SqlServerOperationBuilderExtensions
   {
      /// <summary>
      /// Specifies "include columns"
      /// </summary>
      /// <param name="operation">Operation to specify the include columns for.</param>
      /// <param name="columns">Names of the columns.</param>
      /// <returns>The provided <paramref name="operation"/>.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="operation"/> is <c>null</c>.
      /// - or <paramref name="columns"/> collection is <c>null</c>.
      /// </exception>
      /// <exception cref="ArgumentException"><paramref name="columns"/> collection is empty.</exception>
      [NotNull]
      public static OperationBuilder<CreateIndexOperation> IncludeColumns([NotNull] this OperationBuilder<CreateIndexOperation> operation, [NotNull] params string[] columns)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));
         if (columns == null)
            throw new ArgumentNullException(nameof(columns));
         if (columns.Length == 0)
            throw new ArgumentException("There must be at least one column in provided collection.", nameof(columns));

         operation.Annotation("SqlServer:Include", columns);

         return operation;
      }

      /// <summary>
      /// Specifies the column as an "identity column".
      /// </summary>
      /// <param name="operation">Operation</param>
      /// <returns>The provided <paramref name="operation"/>.</returns>
      /// <exception cref="ArgumentNullException">The provided <paramref name="operation"/> is <c>null</c>.</exception>
      [NotNull]
      public static OperationBuilder<AddColumnOperation> AsIdentityColumn([NotNull] this OperationBuilder<AddColumnOperation> operation)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));

         operation.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

         return operation;
      }

      /// <summary>
      /// Sets the PK as clustered/non-clustered.
      /// </summary>
      /// <param name="operation">Operation</param>
      /// <param name="isClustered">Indication whether the PK should be clustered or not.</param>
      /// <returns>The provided <paramref name="operation"/>.</returns>
      /// <exception cref="ArgumentNullException">The <paramref name="operation"/> is <c>null</c>.</exception>
      [NotNull]
      public static OperationBuilder<AddPrimaryKeyOperation> IsClustered([NotNull] this OperationBuilder<AddPrimaryKeyOperation> operation, bool isClustered = true)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));

         operation.Annotation("SqlServer:Clustered", isClustered);

         return operation;
      }
   }
}
