using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Migrations
{
   /// <summary>
   /// Extended migration SQL generator.
   /// </summary>
   public class ThinktectureSqlServerMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
   {
      /// <summary>
      /// Initializes <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/>.
      /// </summary>
      /// <param name="dependencies">Dependencies.</param>
      /// <param name="migrationsAnnotations">Migration annotations.</param>
      public ThinktectureSqlServerMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, IMigrationsAnnotationProvider migrationsAnnotations)
         : base(dependencies, migrationsAnnotations)
      {
      }

      /// <inheritdoc />
      protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         if (!operation.IfNotExistsCheckRequired())
         {
            base.Generate(operation, model, builder, terminate);
            return;
         }

         builder.AppendLine($"IF(OBJECT_ID('{DelimitIdentifier(operation.Name, operation.Schema)}') IS NULL)")
                .AppendLine("BEGIN");

         using (builder.Indent())
         {
            base.Generate(operation, model, builder, false);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
         }

         builder.AppendLine("END")
                .EndCommand();
      }

      /// <inheritdoc />
      protected override void Generate(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate)
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         if (!operation.IfNotExistsCheckRequired())
         {
            base.Generate(operation, model, builder, terminate);
            return;
         }

         builder.AppendLine($"IF(COL_LENGTH('{DelimitIdentifier(operation.Table, operation.Schema)}', {GenerateSqlLiteral(operation.Name)}) IS NULL)")
                .AppendLine("BEGIN");

         using (builder.Indent())
         {
            base.Generate(operation, model, builder, false);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
         }

         builder.AppendLine("END");

         if (!terminate)
            return;

         if (operation.Comment != null)
            AddDescription(builder, operation.Comment, operation.Schema, operation.Table, operation.Name);

         builder.EndCommand();
      }

      /// <inheritdoc />
      protected override void Generate(DropColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         if (!operation.IfExistsCheckRequired())
         {
            base.Generate(operation, model, builder, terminate);
            return;
         }

         builder.AppendLine($"IF(COL_LENGTH('{DelimitIdentifier(operation.Table, operation.Schema)}', {GenerateSqlLiteral(operation.Name)}) IS NOT NULL)")
                .AppendLine("BEGIN");

         using (builder.Indent())
         {
            base.Generate(operation, model, builder, false);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
         }

         builder.AppendLine("END");

         if (!terminate)
            return;

         builder.EndCommand();
      }

      /// <inheritdoc />
      protected override void Generate(DropIndexOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate)
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         if (!operation.IfExistsCheckRequired())
         {
            base.Generate(operation, model, builder, terminate);
            return;
         }

         builder.AppendLine($"IF(IndexProperty(OBJECT_ID('{DelimitIdentifier(operation.Table, operation.Schema)}'), {GenerateSqlLiteral(operation.Name)}, 'IndexId') IS NOT NULL)")
                .AppendLine("BEGIN");

         using (builder.Indent())
         {
            base.Generate(operation, model, builder, false);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
         }

         builder.AppendLine("END");

         if (!terminate)
            return;

         builder.EndCommand();
      }

      /// <inheritdoc />
      protected override void Generate(CreateIndexOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         if (!operation.IfNotExistsCheckRequired())
         {
            base.Generate(operation, model, builder, terminate);
            return;
         }

         builder.AppendLine($"IF(IndexProperty(OBJECT_ID('{DelimitIdentifier(operation.Table, operation.Schema)}'), {GenerateSqlLiteral(operation.Name)}, 'IndexId') IS NULL)")
                .AppendLine("BEGIN");

         using (builder.Indent())
         {
            base.Generate(operation, model, builder, false);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
         }

         builder.AppendLine("END");

         if (!terminate)
            return;

         builder.EndCommand();
      }

      private string GenerateSqlLiteral(string text)
      {
         return Dependencies.TypeMappingSource.GetMapping(typeof(string)).GenerateSqlLiteral(text);
      }

      private string DelimitIdentifier(string name, string schema)
      {
         return Dependencies.SqlGenerationHelper.DelimitIdentifier(name, schema);
      }
   }
}
