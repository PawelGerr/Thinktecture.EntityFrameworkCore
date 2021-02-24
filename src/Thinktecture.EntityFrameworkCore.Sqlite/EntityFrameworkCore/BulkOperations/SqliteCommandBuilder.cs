using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   internal abstract class SqliteCommandBuilder
   {
      public static readonly SqliteCommandBuilder Insert = new SqliteInsertBuilder();

      public static SqliteCommandBuilder Update(IReadOnlyList<IProperty> keyProperties)
      {
         return new SqliteUpdateBuilder(keyProperties);
      }

      public abstract string GetStatement(
         ISqlGenerationHelper sqlGenerationHelper,
         IEntityDataReader reader,
         string tableIdentifier);

      private class SqliteInsertBuilder : SqliteCommandBuilder
      {
         /// <inheritdoc />
         public override string GetStatement(ISqlGenerationHelper sqlGenerationHelper, IEntityDataReader reader, string tableIdentifier)
         {
            var sb = new StringBuilder();
            sb.Append("INSERT INTO ").Append(tableIdentifier).Append('(');

            for (var i = 0; i < reader.Properties.Count; i++)
            {
               var column = reader.Properties[i];

               if (i > 0)
                  sb.Append(", ");

               sb.Append(sqlGenerationHelper.DelimitIdentifier(column.GetColumnBaseName()));
            }

            sb.Append(") VALUES (");

            for (var i = 0; i < reader.Properties.Count; i++)
            {
               var property = reader.Properties[i];
               var index = reader.GetPropertyIndex(property);

               if (i > 0)
                  sb.Append(", ");

               sb.Append("$p").Append(index);
            }

            sb.Append(")").Append(sqlGenerationHelper.StatementTerminator);

            return sb.ToString();
         }
      }

      private class SqliteUpdateBuilder : SqliteCommandBuilder
      {
         private readonly IReadOnlyList<IProperty> _keyProperties;

         public SqliteUpdateBuilder(IReadOnlyList<IProperty> keyProperties)
         {
            _keyProperties = keyProperties ?? throw new ArgumentNullException(nameof(keyProperties));
         }

         public override string GetStatement(ISqlGenerationHelper sqlGenerationHelper, IEntityDataReader reader, string tableIdentifier)
         {
            var sb = new StringBuilder();
            sb.Append("UPDATE ").Append(tableIdentifier).Append(" SET ");

            var isFirst = true;

            for (var i = 0; i < reader.Properties.Count; i++)
            {
               var property = reader.Properties[i];

               if (_keyProperties.Contains(property))
                  continue;

               if (!isFirst)
                  sb.Append(", ");

               sb.Append(sqlGenerationHelper.DelimitIdentifier(property.GetColumnBaseName()))
                 .Append(" = $p").Append(i);

               isFirst = false;
            }

            sb.Append(" WHERE ");

            for (var i = 0; i < _keyProperties.Count; i++)
            {
               if (i > 0)
                  sb.Append(" AND ");

               var property = _keyProperties[i];
               var index = reader.GetPropertyIndex(property);
               var escapedColumnName = sqlGenerationHelper.DelimitIdentifier(property.GetColumnBaseName());

               sb.Append("(").Append(escapedColumnName).Append(" = $p").Append(index);

               if (property.IsNullable)
                  sb.Append(" OR ").Append(escapedColumnName).Append(" IS NULL AND $p").Append(index).Append(" IS NULL");

               sb.Append(")");
            }

            sb.Append(sqlGenerationHelper.StatementTerminator);

            return sb.ToString();
         }
      }
   }
}
