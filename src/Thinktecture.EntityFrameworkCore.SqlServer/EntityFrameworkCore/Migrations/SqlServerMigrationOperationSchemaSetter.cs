using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;

namespace Thinktecture.EntityFrameworkCore.Migrations;

/// <summary>
/// Applies the schema to operations.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class SqlServerMigrationOperationSchemaSetter : MigrationOperationSchemaSetter
{
   /// <inheritdoc />
   protected override void SetSchema(CreateTableOperation op, string schema)
   {
      base.SetSchema(op, schema);

      var isTemporal = op[SqlServerAnnotationNames.IsTemporal] as bool? == true;

      if (isTemporal)
         op[SqlServerAnnotationNames.TemporalHistoryTableSchema] ??= schema;
   }
}
