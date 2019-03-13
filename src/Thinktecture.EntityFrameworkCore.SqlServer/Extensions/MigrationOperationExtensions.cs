using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   public static class MigrationOperationExtensions
   {
      public static bool IfNotExistsCheckRequired([NotNull] this MigrationOperation operation)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));

         return operation[OperationBuilderExtensions.IfNotExistsKey] is bool ifNotExists && ifNotExists;
      }

      public static bool IfExistsCheckRequired([NotNull] this MigrationOperation operation)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));

         return operation[OperationBuilderExtensions.IfExistsKey] is bool ifNotExists && ifNotExists;
      }
   }
}
