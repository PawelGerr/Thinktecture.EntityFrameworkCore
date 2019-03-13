using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Migrations
{
   public class ThinktectureSqlServerMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
   {
      public ThinktectureSqlServerMigrationsSqlGenerator([NotNull] MigrationsSqlGeneratorDependencies dependencies, [NotNull] IMigrationsAnnotationProvider migrationsAnnotations)
         : base(dependencies, migrationsAnnotations)
      {
      }

      /// <inheritdoc />
      protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder)
      {
         if (!operation.IfNotExistsCheckRequired())
         {
            base.Generate(operation, model, builder);
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
      protected override void Generate(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
      {
         if (!operation.IfNotExistsCheckRequired())
         {
            base.Generate(operation, model, builder);
            return;
         }

         builder.AppendLine($"IF(COL_LENGTH('{DelimitIdentifier(operation.Table, operation.Schema)}', {GenerateSqlLiteral(operation.Name)}) IS NULL)")
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
      protected override void Generate(DropColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
      {
         if (!operation.IfExistsCheckRequired())
         {
            base.Generate(operation, model, builder);
            return;
         }

         builder.AppendLine($"IF(COL_LENGTH('{DelimitIdentifier(operation.Table, operation.Schema)}', {GenerateSqlLiteral(operation.Name)}) IS NOT NULL)")
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
      protected override void Generate(DropIndexOperation operation, IModel model, MigrationCommandListBuilder builder)
      {
         if (!operation.IfExistsCheckRequired())
         {
            base.Generate(operation, model, builder);
            return;
         }

         builder.AppendLine($"IF(IndexProperty(OBJECT_ID('{DelimitIdentifier(operation.Table, operation.Schema)}'), {GenerateSqlLiteral(operation.Name)}, 'IndexId') IS NOT NULL)")
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
      protected override void Generate(CreateIndexOperation operation, IModel model, MigrationCommandListBuilder builder)
      {
         if (!operation.IfNotExistsCheckRequired())
         {
            base.Generate(operation, model, builder);
            return;
         }

         builder.AppendLine($"IF(IndexProperty(OBJECT_ID('{DelimitIdentifier(operation.Table, operation.Schema)}'), {GenerateSqlLiteral(operation.Name)}, 'IndexId') IS NULL)")
                .AppendLine("BEGIN");

         using (builder.Indent())
         {
            base.Generate(operation, model, builder, false);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
         }

         builder.AppendLine("END")
                .EndCommand();
      }

      private string GenerateSqlLiteral([CanBeNull] string text)
      {
         return Dependencies.TypeMappingSource.GetMapping(typeof(string)).GenerateSqlLiteral(text);
      }

      private string DelimitIdentifier([NotNull] string name, string schema)
      {
         return Dependencies.SqlGenerationHelper.DelimitIdentifier(name, schema);
      }
   }
}
