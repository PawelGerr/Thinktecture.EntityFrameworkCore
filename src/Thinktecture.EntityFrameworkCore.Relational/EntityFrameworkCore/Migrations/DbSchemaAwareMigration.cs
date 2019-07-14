using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Thinktecture.EntityFrameworkCore.Migrations
{
   /// <summary>
   /// A migration that takes the schema into account.
   /// </summary>
   public abstract class DbSchemaAwareMigration : Migration
   {
      private readonly Lazy<IReadOnlyList<MigrationOperation>> _upLazy;
      private readonly Lazy<IReadOnlyList<MigrationOperation>> _downLazy;

      /// <summary>
      /// Database schema to use.
      /// </summary>
      protected string Schema { get; }

      /// <inheritdoc />
      [NotNull]
      public override IReadOnlyList<MigrationOperation> UpOperations => _upLazy.Value;

      /// <inheritdoc />
      [NotNull]
      public override IReadOnlyList<MigrationOperation> DownOperations => _downLazy.Value;

      /// <summary>
      /// Initializes a new instance of <see cref="DbSchemaAwareMigration"/>.
      /// </summary>
      /// <param name="schema">Schema to use.</param>
      protected DbSchemaAwareMigration([CanBeNull] IDbContextSchema schema)
      {
         Schema = schema?.Schema;
         _upLazy = new Lazy<IReadOnlyList<MigrationOperation>>(() => SetSchema(base.UpOperations));
         _downLazy = new Lazy<IReadOnlyList<MigrationOperation>>(() => SetSchema(base.DownOperations));
      }

      [NotNull]
      private IReadOnlyList<MigrationOperation> SetSchema([NotNull] IReadOnlyList<MigrationOperation> operations)
      {
         if (operations == null)
            throw new ArgumentNullException(nameof(operations));

         if (Schema == null)
            return operations;

         return operations.Select(SetSchema).ToList().AsReadOnly();
      }

      [NotNull]
      private MigrationOperation SetSchema([NotNull] MigrationOperation operation)
      {
         if (operation == null)
            throw new ArgumentNullException(nameof(operation));

         switch (operation)
         {
            case CreateTableOperation createTable:
               SetSchema(createTable);
               break;
            case RenameTableOperation renameTable:
               SetSchema(renameTable);
               break;
            case AddForeignKeyOperation addForeignKey:
               SetSchema(addForeignKey);
               break;
            default:
               var opType = operation.GetType();
#pragma warning disable CA1507
               SetSchema(operation, opType, "Schema");
#pragma warning restore CA1507
               SetSchema(operation, opType, "PrincipalSchema");
               break;
         }

         return operation;
      }

      private void SetSchema([NotNull] CreateTableOperation op)
      {
         if (op == null)
            throw new ArgumentNullException(nameof(op));

         op.Schema = Schema;
         SetSchema(op.PrimaryKey);

         foreach (var column in op.Columns)
         {
            SetSchema(column);
         }

         foreach (var key in op.ForeignKeys)
         {
            SetSchema(key);
         }

         foreach (var key in op.UniqueConstraints)
         {
            SetSchema(key);
         }
      }

      private void SetSchema([NotNull] RenameTableOperation op)
      {
         if (op == null)
            throw new ArgumentNullException(nameof(op));

         if (op.Schema == null)
            op.Schema = Schema;

         if (op.NewSchema == null)
            op.NewSchema = Schema;
      }

      private void SetSchema([NotNull] AddForeignKeyOperation op)
      {
         if (op == null)
            throw new ArgumentNullException(nameof(op));

         if (op.Schema == null)
            op.Schema = Schema;

         if (op.PrincipalSchema == null)
            op.PrincipalSchema = Schema;
      }

      private void SetSchema([NotNull] MigrationOperation operation, [NotNull] Type opType, [NotNull] string propertyName)
      {
         var propInfo = opType.GetProperty(propertyName);

         if (propInfo != null && propInfo.GetValue(operation) == null)
            propInfo.SetValue(operation, Schema);
      }
   }
}
