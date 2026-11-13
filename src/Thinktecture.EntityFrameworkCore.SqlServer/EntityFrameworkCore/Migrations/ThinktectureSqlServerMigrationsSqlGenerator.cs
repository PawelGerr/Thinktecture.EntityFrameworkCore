using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

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
   /// <param name="commandBatchPreparer">The command batch preparer.</param>
   public ThinktectureSqlServerMigrationsSqlGenerator(
      MigrationsSqlGeneratorDependencies dependencies,
      ICommandBatchPreparer commandBatchPreparer)
      : base(dependencies, commandBatchPreparer)
   {
   }

   /// <inheritdoc />
   protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
   {
      ArgumentNullException.ThrowIfNull(builder);

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
      ArgumentNullException.ThrowIfNull(builder);

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
      ArgumentNullException.ThrowIfNull(builder);

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
      ArgumentNullException.ThrowIfNull(builder);

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
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfNotExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      EnsureNoFullTextIndex(operation, nameof(SqlServerOperationBuilderExtensions.IfNotExists));

      builder.AppendLine($"IF(IndexProperty(OBJECT_ID('{DelimitIdentifier(operation.Table, operation.Schema)}'), {GenerateSqlLiteral(operation.Name)}, 'IndexId') IS NULL)")
             .AppendLine("BEGIN");

      using (builder.Indent())
      {
         base.Generate(operation, model, builder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END");

      if (terminate)
         builder.EndCommand(IsTransactionSuppressed(probe => base.Generate(operation, model, probe)));
   }

   /// <inheritdoc />
   protected override void Generate(DropIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate)
   {
      ArgumentNullException.ThrowIfNull(builder);

      if (operation.IfNotExistsCheckRequired())
         throw new InvalidOperationException($"The check '{nameof(SqlServerOperationBuilderExtensions.IfNotExists)}()' is not allowed with '{operation.GetType().Name}'");

      if (!operation.IfExistsCheckRequired())
      {
         base.Generate(operation, model, builder, terminate);
         return;
      }

      var table = operation.Table ?? throw new InvalidOperationException($"The {nameof(DropIndexOperation)} for index '{operation.Name}' has no table name.");
      EnsureNoFullTextIndex(operation, nameof(SqlServerOperationBuilderExtensions.IfExists));

      builder.AppendLine($"IF(IndexProperty(OBJECT_ID('{DelimitIdentifier(table, operation.Schema)}'), {GenerateSqlLiteral(operation.Name)}, 'IndexId') IS NOT NULL)")
             .AppendLine("BEGIN");

      using (builder.Indent())
      {
         base.Generate(operation, model, builder, false);
         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
      }

      builder.AppendLine("END");

      if (terminate)
         builder.EndCommand(IsTransactionSuppressed(probe => base.Generate(operation, model, probe, true)));
   }

   /// <inheritdoc />
   protected override void Generate(AddUniqueConstraintOperation operation, IModel? model, MigrationCommandListBuilder builder)
   {
      ArgumentNullException.ThrowIfNull(builder);

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
      ArgumentNullException.ThrowIfNull(builder);

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
      ArgumentNullException.ThrowIfNull(builder);

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

   // EF Core runs some index statements (e.g. full-text, vector, JSON, memory-optimized) outside of a transaction; the wrapped statement must keep that.
   private bool IsTransactionSuppressed(Action<MigrationCommandListBuilder> generateTerminated)
   {
      var probe = new MigrationCommandListBuilder(Dependencies);
      generateTerminated(probe);

      return probe.GetCommandList().Any(command => command.TransactionSuppressed);
   }

   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   private static void EnsureNoFullTextIndex(MigrationOperation operation, string check)
   {
      if (operation[SqlServerAnnotationNames.FullTextIndex] is not null)
         throw new NotSupportedException($"The check '{check}()' is not supported for a full-text index, because a full-text index cannot be found by its name.");
   }
}
