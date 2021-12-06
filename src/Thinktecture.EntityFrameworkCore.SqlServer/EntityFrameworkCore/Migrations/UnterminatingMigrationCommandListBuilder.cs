using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.EntityFrameworkCore.Migrations;

/// <summary>
/// A hack preventing premature end of command.
/// </summary>
internal class UnterminatingMigrationCommandListBuilder : MigrationCommandListBuilder
{
   private readonly MigrationCommandListBuilder _builder;

   public bool? SuppressTransaction { get; private set; }

   public UnterminatingMigrationCommandListBuilder(
      MigrationCommandListBuilder builder,
      MigrationsSqlGeneratorDependencies dependencies)
      : base(dependencies)
   {
      _builder = builder ?? throw new ArgumentNullException(nameof(builder));
   }

   public override MigrationCommandListBuilder EndCommand(bool suppressTransaction = false)
   {
      if (SuppressTransaction.HasValue)
         throw new NotSupportedException($"Multiple calls to '{nameof(EndCommand)}' detected. The methods '{nameof(SqlServerOperationBuilderExtensions.IfExists)}()'/'{nameof(SqlServerOperationBuilderExtensions.IfNotExists)}()' cannot be used with current migration operation.");

      SuppressTransaction = suppressTransaction;
      return this;
   }

   public override MigrationCommandListBuilder Append(string o)
   {
      _builder.Append(o);
      return this;
   }

   public override IDisposable Indent()
   {
      return _builder.Indent();
   }

   public override MigrationCommandListBuilder AppendLine()
   {
      _builder.AppendLine();
      return this;
   }

   public override MigrationCommandListBuilder AppendLine(string o)
   {
      _builder.AppendLine(o);
      return this;
   }

   public override MigrationCommandListBuilder AppendLines(string o)
   {
      _builder.AppendLines(o);
      return this;
   }

   public override MigrationCommandListBuilder DecrementIndent()
   {
      _builder.DecrementIndent();
      return this;
   }

   public override MigrationCommandListBuilder IncrementIndent()
   {
      _builder.IncrementIndent();
      return this;
   }

   public override IReadOnlyList<MigrationCommand> GetCommandList()
   {
      return _builder.GetCommandList();
   }
}