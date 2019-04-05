using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   public static class OperationBuilderExtensions
   {
      public static readonly string IfNotExistsKey = "Thinktecture:OperationBuilderExtensions:IfNotExists";
      public static readonly string IfExistsKey = "Thinktecture:OperationBuilderExtensions:IfExists";

      public static void IfNotExists<T>([NotNull] this OperationBuilder<T> builder)
         where T : MigrationOperation
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         builder.Annotation(IfNotExistsKey, true);
      }

      public static void IfExists<T>([NotNull] this OperationBuilder<T> builder)
         where T : MigrationOperation
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         builder.Annotation(IfExistsKey, true);
      }
   }
}
