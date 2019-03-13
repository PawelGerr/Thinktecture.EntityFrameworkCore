using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   public static class SqlServerOperationBuilderExtensions
   {
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
