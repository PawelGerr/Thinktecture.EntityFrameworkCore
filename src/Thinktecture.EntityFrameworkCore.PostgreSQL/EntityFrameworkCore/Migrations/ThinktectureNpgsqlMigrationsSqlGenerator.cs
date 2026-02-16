using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace Thinktecture.EntityFrameworkCore.Migrations;

/// <summary>
/// Extended migration SQL generator for PostgreSQL.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureNpgsqlMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
{
   private bool _closeScopeBeforeEndingStatement;

   /// <summary>
   /// Initializes <see cref="ThinktectureNpgsqlMigrationsSqlGenerator"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   /// <param name="npgsqlSingletonOptions">Npgsql singleton options.</param>
   public ThinktectureNpgsqlMigrationsSqlGenerator(
      MigrationsSqlGeneratorDependencies dependencies,
      INpgsqlSingletonOptions npgsqlSingletonOptions)
      : base(dependencies, npgsqlSingletonOptions)
   {
   }

   /// <inheritdoc />
   protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(NpgsqlOperationBuilderExtensions.IfExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfNotExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      var schemaExpression = GetSchemaExpression(operation.Schema);

      builder.AppendLine("DO $$")
             .AppendLine("BEGIN")
             .AppendLine($"IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = {schemaExpression} AND table_name = {GenerateSqlLiteral(operation.Name)}) THEN");

      var unterminatingBuilder = new UnterminatingMigrationCommandListBuilder(builder, Dependencies);

      using (builder.Indent())
      {
         base.Generate(operation, model, unterminatingBuilder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END IF;")
             .AppendLine("END")
             .AppendLine("$$;");

      if (terminate || unterminatingBuilder.SuppressTransaction.HasValue)
         builder.EndCommand(unterminatingBuilder.SuppressTransaction ?? false);
   }

   /// <inheritdoc />
   protected override void Generate(DropTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfNotExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(NpgsqlOperationBuilderExtensions.IfNotExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      var schemaExpression = GetSchemaExpression(operation.Schema);

      builder.AppendLine("DO $$")
             .AppendLine("BEGIN")
             .AppendLine($"IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = {schemaExpression} AND table_name = {GenerateSqlLiteral(operation.Name)}) THEN");

      using (builder.Indent())
      {
         base.Generate(operation, model, builder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END IF;")
             .AppendLine("END")
             .AppendLine("$$;");

      if (terminate)
         builder.EndCommand();
   }

   /// <inheritdoc />
   protected override void Generate(AddColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(NpgsqlOperationBuilderExtensions.IfExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfNotExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      var schemaExpression = GetSchemaExpression(operation.Schema);

      builder.AppendLine("DO $$")
             .AppendLine("BEGIN")
             .AppendLine($"IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = {schemaExpression} AND table_name = {GenerateSqlLiteral(operation.Table)} AND column_name = {GenerateSqlLiteral(operation.Name)}) THEN");

      using (builder.Indent())
      {
         base.Generate(operation, model, builder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END IF;")
             .AppendLine("END")
             .AppendLine("$$;");

      if (terminate)
         builder.EndCommand();
   }

   /// <inheritdoc />
   protected override void Generate(DropColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfNotExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(NpgsqlOperationBuilderExtensions.IfNotExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      var schemaExpression = GetSchemaExpression(operation.Schema);

      builder.AppendLine("DO $$")
             .AppendLine("BEGIN")
             .AppendLine($"IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = {schemaExpression} AND table_name = {GenerateSqlLiteral(operation.Table)} AND column_name = {GenerateSqlLiteral(operation.Name)}) THEN");

      using (builder.Indent())
      {
         base.Generate(operation, model, builder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END IF;")
             .AppendLine("END")
             .AppendLine("$$;");

      if (terminate)
         builder.EndCommand();
   }

   /// <inheritdoc />
   protected override void Generate(CreateIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(NpgsqlOperationBuilderExtensions.IfExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfNotExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      var schemaExpression = GetSchemaExpression(operation.Schema);

      builder.AppendLine("DO $$")
             .AppendLine("BEGIN")
             .AppendLine($"IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = {schemaExpression} AND indexname = {GenerateSqlLiteral(operation.Name)}) THEN");

      using (builder.Indent())
      {
         base.Generate(operation, model, builder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END IF;")
             .AppendLine("END")
             .AppendLine("$$;");

      if (terminate)
         builder.EndCommand();
   }

   /// <inheritdoc />
   protected override void Generate(DropIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfNotExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(NpgsqlOperationBuilderExtensions.IfNotExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      var schemaExpression = GetSchemaExpression(operation.Schema);

      builder.AppendLine("DO $$")
             .AppendLine("BEGIN")
             .AppendLine($"IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = {schemaExpression} AND indexname = {GenerateSqlLiteral(operation.Name)}) THEN");

      using (builder.Indent())
      {
         base.Generate(operation, model, builder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END IF;")
             .AppendLine("END")
             .AppendLine("$$;");

      if (terminate)
         builder.EndCommand();
   }

   /// <inheritdoc />
   protected override void Generate(AddUniqueConstraintOperation operation, IModel? model, MigrationCommandListBuilder builder)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(NpgsqlOperationBuilderExtensions.IfExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfNotExistsCheckRequired())
      {
         base.Generate(operation, model, builder);
         return;
      }

      var schemaExpression = GetSchemaExpression(operation.Schema);

      builder.AppendLine("DO $$")
             .AppendLine("BEGIN")
             .AppendLine($"IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_schema = {schemaExpression} AND constraint_name = {GenerateSqlLiteral(operation.Name)} AND constraint_type = 'UNIQUE') THEN");

      _closeScopeBeforeEndingStatement = true;
      builder.IncrementIndent();

      base.Generate(operation, model, builder);
   }

   /// <inheritdoc />
   protected override void Generate(DropUniqueConstraintOperation operation, IModel? model, MigrationCommandListBuilder builder)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfNotExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(NpgsqlOperationBuilderExtensions.IfNotExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfExistsCheckRequired())
      {
         base.Generate(operation, model, builder);
         return;
      }

      var schemaExpression = GetSchemaExpression(operation.Schema);

      builder.AppendLine("DO $$")
             .AppendLine("BEGIN")
             .AppendLine($"IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_schema = {schemaExpression} AND constraint_name = {GenerateSqlLiteral(operation.Name)} AND constraint_type = 'UNIQUE') THEN");

      _closeScopeBeforeEndingStatement = true;
      builder.IncrementIndent();

      base.Generate(operation, model, builder);
   }

   /// <inheritdoc />
   protected override void EndStatement(MigrationCommandListBuilder builder, bool suppressTransaction = false)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (_closeScopeBeforeEndingStatement)
      {
         _closeScopeBeforeEndingStatement = false;

         builder.DecrementIndent();
         builder.AppendLine("END IF;")
                .AppendLine("END")
                .AppendLine("$$;");
      }

      base.EndStatement(builder, suppressTransaction);
   }

   private string GenerateSqlLiteral(string text)
   {
      return Dependencies.TypeMappingSource.GetMapping(typeof(string)).GenerateSqlLiteral(text);
   }

   private string GetSchemaExpression(string? schema)
   {
      return schema is not null ? GenerateSqlLiteral(schema) : "current_schema()";
   }
}
