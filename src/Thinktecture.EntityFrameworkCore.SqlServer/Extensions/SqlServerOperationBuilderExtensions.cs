using System;
using JetBrains.Annotations;
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
      /// <typeparam name="T">Type of the operation.</typeparam>
      /// <returns>The provided <paramref name="operation"/>.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="operation"/> is <c>null</c>.
      /// - or <paramref name="columns"/> collection is <c>null</c>.
      /// </exception>
      /// <exception cref="ArgumentException"><paramref name="columns"/> collection is empty.</exception>
      [NotNull]
      public static OperationBuilder<T> IncludeColumns<T>([NotNull] this OperationBuilder<T> operation, [NotNull] params string[] columns)
         where T : MigrationOperation
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
   }
}
