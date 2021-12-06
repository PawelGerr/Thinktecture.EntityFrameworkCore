using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Migrations;

/// <summary>
/// Extended migration SQL generator.
/// </summary>
public class ThinktectureSqlServerMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
{
   private bool _closeScopeBeforeEndingStatement;

   /// <summary>
   /// Initializes <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   /// <param name="migrationsAnnotations">Migration annotations.</param>
   public ThinktectureSqlServerMigrationsSqlGenerator(
      MigrationsSqlGeneratorDependencies dependencies,
      IRelationalAnnotationProvider migrationsAnnotations)
      : base(dependencies, migrationsAnnotations)
   {
   }

   /// <inheritdoc />
   protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      if (operation.IfExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfNotExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      builder.AppendLine($"IF(OBJECT_ID('{DelimitIdentifier(operation.Name, operation.Schema)}') IS NULL)")
             .AppendLine("BEGIN");

      var unterminatingBuilder = new UnterminatingMigrationCommandListBuilder(builder, Dependencies);

      using (builder.Indent())
      {
         base.Generate(operation, model, unterminatingBuilder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END");

      if (terminate || unterminatingBuilder.SuppressTransaction.HasValue)
         builder.EndCommand(unterminatingBuilder.SuppressTransaction ?? false);
   }

   /// <inheritdoc />
   protected override void Generate(DropTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      if (operation.IfNotExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfNotExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      builder.AppendLine($"IF(OBJECT_ID('{DelimitIdentifier(operation.Name, operation.Schema)}') IS NOT NULL)")
             .AppendLine("BEGIN");

      using (builder.Indent())
      {
         base.Generate(operation, model, builder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END");

      if (terminate)
         builder.EndCommand();
   }

   /// <inheritdoc />
   protected override void Generate(AddColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      if (operation.IfExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfExists)}()' is not allowed with '{operation.GetType().Name}'");

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
   protected override void Generate(DropColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      if (operation.IfNotExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfNotExists)}()' is not allowed with '{operation.GetType().Name}'");

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

      if (terminate)
         builder.EndCommand();
   }

   /// <inheritdoc />
   protected override void Generate(CreateIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      if (operation.IfExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfExists)}()' is not allowed with '{operation.GetType().Name}'");

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

      if (terminate)
         builder.EndCommand();
   }

   /// <inheritdoc />
   protected override void Generate(DropIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      if (operation.IfNotExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfNotExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      var table = operation.Table ?? throw new InvalidOperationException($"The {nameof(DropIndexOperation)} for index '{operation.Name}' has no table name.");

      builder.AppendLine($"IF(IndexProperty(OBJECT_ID('{DelimitIdentifier(table, operation.Schema)}'), {GenerateSqlLiteral(operation.Name)}, 'IndexId') IS NOT NULL)")
             .AppendLine("BEGIN");

      using (builder.Indent())
      {
         base.Generate(operation, model, builder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END");

      if (terminate)
         builder.EndCommand();
   }

   /// <inheritdoc />
   protected override void Generate(AddUniqueConstraintOperation operation, IModel? model, MigrationCommandListBuilder builder)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      if (operation.IfExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfNotExistsCheckRequired())
      {
         base.Generate(operation, model, builder);
         return;
      }

      builder.AppendLine($"IF(OBJECT_ID('{DelimitIdentifier(operation.Name)}') IS NULL)")
             .AppendLine("BEGIN");

      _closeScopeBeforeEndingStatement = true;
      builder.IncrementIndent();

      base.Generate(operation, model, builder);
   }

   /// <inheritdoc />
   protected override void Generate(DropUniqueConstraintOperation operation, IModel? model, MigrationCommandListBuilder builder)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      if (operation.IfNotExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfNotExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfExistsCheckRequired())
      {
         base.Generate(operation, model, builder);
         return;
      }

      builder.AppendLine($"IF(OBJECT_ID('{DelimitIdentifier(operation.Name)}') IS NOT NULL)")
             .AppendLine("BEGIN");

      _closeScopeBeforeEndingStatement = true;
      builder.IncrementIndent();

      base.Generate(operation, model, builder);
   }

   /// <inheritdoc />
   protected override void EndStatement(MigrationCommandListBuilder builder, bool suppressTransaction = false)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      if (_closeScopeBeforeEndingStatement)
      {
         _closeScopeBeforeEndingStatement = false;

         builder.DecrementIndent();
         builder.AppendLine("END");
      }

      base.EndStatement(builder, suppressTransaction);
   }

   private string GenerateSqlLiteral(string text)
   {
      return Dependencies.TypeMappingSource.GetMapping(typeof(string)).GenerateSqlLiteral(text);
   }

   private string DelimitIdentifier(string name, string? schema = null)
   {
      return Dependencies.SqlGenerationHelper.DelimitIdentifier(name, schema);
   }
}