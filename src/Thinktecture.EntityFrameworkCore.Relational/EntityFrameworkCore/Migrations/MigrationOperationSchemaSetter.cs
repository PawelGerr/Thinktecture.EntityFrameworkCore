using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Thinktecture.EntityFrameworkCore.Migrations
{
   /// <summary>
   /// Applies the schema to operations.
   /// </summary>
   public class MigrationOperationSchemaSetter : IMigrationOperationSchemaSetter
   {
      /// <inheritdoc />
      public void SetSchema(IReadOnlyList<MigrationOperation> operations, string schema)
      {
         if (operations == null)
            throw new ArgumentNullException(nameof(operations));

         foreach (var operation in operations)
         {
            SetSchema(operation, schema);
         }
      }

      private static void SetSchema([NotNull] MigrationOperation operation, [CanBeNull] string schema)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));

         switch (operation)
         {
            case CreateTableOperation createTable:
               SetSchema(createTable, schema);
               break;
            case RenameTableOperation renameTable:
               SetSchema(renameTable, schema);
               break;
            case AddForeignKeyOperation addForeignKey:
               SetSchema(addForeignKey, schema);
               break;
            default:
               var opType = operation.GetType();
#pragma warning disable CA1507
               SetSchema(operation, opType, "Schema", schema);
#pragma warning restore CA1507
               SetSchema(operation, opType, "PrincipalSchema", schema);
               break;
         }
      }

      private static void SetSchema([NotNull] CreateTableOperation op, [CanBeNull] string schema)
      {
         if (op == null)
            throw new ArgumentNullException(nameof(op));

         op.Schema = schema;
         SetSchema(op.PrimaryKey, schema);

         foreach (var column in op.Columns)
         {
            SetSchema(column, schema);
         }

         foreach (var key in op.ForeignKeys)
         {
            SetSchema(key, schema);
         }

         foreach (var key in op.UniqueConstraints)
         {
            SetSchema(key, schema);
         }
      }

      private static void SetSchema([NotNull] RenameTableOperation op, [CanBeNull] string schema)
      {
         if (op == null)
            throw new ArgumentNullException(nameof(op));

         if (op.Schema == null)
            op.Schema = schema;

         if (op.NewSchema == null)
            op.NewSchema = schema;
      }

      private static void SetSchema([NotNull] AddForeignKeyOperation op, [CanBeNull] string schema)
      {
         if (op == null)
            throw new ArgumentNullException(nameof(op));

         if (op.Schema == null)
            op.Schema = schema;

         if (op.PrincipalSchema == null)
            op.PrincipalSchema = schema;
      }

      private static void SetSchema([NotNull] MigrationOperation operation, [NotNull] Type opType, [NotNull] string propertyName, [CanBeNull] string schema)
      {
         var propInfo = opType.GetProperty(propertyName);

         if (propInfo != null && propInfo.GetValue(operation) == null)
            propInfo.SetValue(operation, schema);
      }
   }
}
