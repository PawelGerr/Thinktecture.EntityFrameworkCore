using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal abstract class SqliteCommandBuilder
{
   public static SqliteCommandBuilder Insert(IReadOnlyList<PropertyWithNavigations> propertiesToInsert)
   {
      return new SqliteInsertBuilder(propertiesToInsert);
   }

   public static SqliteCommandBuilder Update(IReadOnlyList<PropertyWithNavigations> propertiesToUpdate, IReadOnlyList<PropertyWithNavigations> keyProperties)
   {
      return new SqliteUpdateBuilder(propertiesToUpdate, keyProperties);
   }

   public static SqliteCommandBuilder InsertOrUpdate(IReadOnlyList<PropertyWithNavigations> propertiesToInsert, IReadOnlyList<PropertyWithNavigations> propertiesToUpdate, IReadOnlyList<PropertyWithNavigations> keyProperties)
   {
      return new SqliteInsertOrUpdateBuilder(propertiesToInsert, propertiesToUpdate, keyProperties);
   }

   private static void GenerateInsertStatement(
      StringBuilder sb,
      ISqlGenerationHelper sqlGenerationHelper,
      IEntityDataReader reader,
      string tableIdentifier,
      IReadOnlyList<PropertyWithNavigations> propertiesToInsert)
   {
      sb.Append("INSERT INTO ").Append(tableIdentifier).Append('(');

      for (var i = 0; i < propertiesToInsert.Count; i++)
      {
         if (i > 0)
            sb.Append(", ");

         var property = propertiesToInsert[i];
         var storeObject = StoreObjectIdentifier.Create(property.Property.DeclaringEntityType, StoreObjectType.Table)
                           ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{property.Property.DeclaringEntityType.Name}'.");
         var columnName = property.Property.GetColumnName(storeObject)
                          ?? throw new Exception($"The property '{property.Property.Name}' has no column name.");

         sb.Append(sqlGenerationHelper.DelimitIdentifier(columnName));
      }

      sb.AppendLine(")")
        .Append("VALUES (");

      for (var i = 0; i < propertiesToInsert.Count; i++)
      {
         var index = reader.GetPropertyIndex(propertiesToInsert[i]);

         if (i > 0)
            sb.Append(", ");

         sb.Append("$p").Append(index);
      }

      sb.Append(")");
   }

   public abstract string GetStatement(
      ISqlGenerationHelper sqlGenerationHelper,
      IEntityDataReader reader,
      string tableIdentifier);

   private class SqliteInsertBuilder : SqliteCommandBuilder
   {
      private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToInsert;

      public SqliteInsertBuilder(IReadOnlyList<PropertyWithNavigations> propertiesToInsert)
      {
         _propertiesToInsert = propertiesToInsert;
      }

      /// <inheritdoc />
      public override string GetStatement(ISqlGenerationHelper sqlGenerationHelper, IEntityDataReader reader, string tableIdentifier)
      {
         var sb = new StringBuilder();
         GenerateInsertStatement(sb, sqlGenerationHelper, reader, tableIdentifier, _propertiesToInsert);

         sb.Append(sqlGenerationHelper.StatementTerminator);

         return sb.ToString();
      }
   }

   private class SqliteUpdateBuilder : SqliteCommandBuilder
   {
      private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToUpdate;
      private readonly IReadOnlyList<PropertyWithNavigations> _keyProperties;

      public SqliteUpdateBuilder(
         IReadOnlyList<PropertyWithNavigations> propertiesToUpdate,
         IReadOnlyList<PropertyWithNavigations> keyProperties)
      {
         _propertiesToUpdate = propertiesToUpdate;
         _keyProperties = keyProperties;
      }

      public override string GetStatement(ISqlGenerationHelper sqlGenerationHelper, IEntityDataReader reader, string tableIdentifier)
      {
         var sb = new StringBuilder();
         sb.Append("UPDATE ").AppendLine(tableIdentifier);

         GenerateSetAndWhereClause(sb, sqlGenerationHelper, reader, _propertiesToUpdate, _keyProperties);

         sb.Append(sqlGenerationHelper.StatementTerminator);

         return sb.ToString();
      }

      public static bool GenerateSetAndWhereClause(
         StringBuilder sb,
         ISqlGenerationHelper sqlGenerationHelper,
         IEntityDataReader reader,
         IReadOnlyList<PropertyWithNavigations> propertiesToUpdate,
         IReadOnlyList<PropertyWithNavigations> keyProperties)
      {
         var isFirst = true;

         foreach (var property in propertiesToUpdate.Except(keyProperties))
         {
            if (!isFirst)
            {
               sb.Append(", ");
            }
            else
            {
               sb.Append("SET ");
            }

            var storeObject = StoreObjectIdentifier.Create(property.Property.DeclaringEntityType, StoreObjectType.Table)
                              ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{property.Property.DeclaringEntityType.Name}'.");
            var columnName = property.Property.GetColumnName(storeObject)
                             ?? throw new Exception($"The property '{property.Property.Name}' has no column name.");
            sb.Append(sqlGenerationHelper.DelimitIdentifier(columnName))
              .Append(" = $p").Append(reader.GetPropertyIndex(property));

            isFirst = false;
         }

         // if no properies to update return
         if (isFirst)
            return false;

         sb.Append(" WHERE ");

         for (var i = 0; i < keyProperties.Count; i++)
         {
            if (i > 0)
               sb.Append(" AND ");

            var property = keyProperties[i];
            var index = reader.GetPropertyIndex(property);
            var storeObject = StoreObjectIdentifier.Create(property.Property.DeclaringEntityType, StoreObjectType.Table)
                              ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{property.Property.DeclaringEntityType.Name}'.");
            var columnName = property.Property.GetColumnName(storeObject)
                             ?? throw new Exception($"The property '{property.Property.Name}' has no column name.");
            var escapedColumnName = sqlGenerationHelper.DelimitIdentifier(columnName);

            sb.Append("(").Append(escapedColumnName).Append(" = $p").Append(index);

            if (property.Property.IsNullable)
               sb.Append(" OR ").Append(escapedColumnName).Append(" IS NULL AND $p").Append(index).Append(" IS NULL");

            sb.Append(")");
         }

         return true;
      }
   }

   private class SqliteInsertOrUpdateBuilder : SqliteCommandBuilder
   {
      private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToInsert;
      private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToUpdate;
      private readonly IReadOnlyList<PropertyWithNavigations> _keyProperties;

      public SqliteInsertOrUpdateBuilder(
         IReadOnlyList<PropertyWithNavigations> propertiesToInsert,
         IReadOnlyList<PropertyWithNavigations> propertiesToUpdate,
         IReadOnlyList<PropertyWithNavigations> keyProperties)
      {
         _propertiesToInsert = propertiesToInsert;
         _propertiesToUpdate = propertiesToUpdate;
         _keyProperties = keyProperties;
      }

      public override string GetStatement(ISqlGenerationHelper sqlGenerationHelper, IEntityDataReader reader, string tableIdentifier)
      {
         var sb = new StringBuilder();
         GenerateInsertStatement(sb, sqlGenerationHelper, reader, tableIdentifier, _propertiesToInsert);

         sb.AppendLine()
           .Append("\tON CONFLICT(");

         for (var i = 0; i < _keyProperties.Count; i++)
         {
            if (i > 0)
               sb.Append(", ");

            var property = _keyProperties[i];
            var storeObject = StoreObjectIdentifier.Create(property.Property.DeclaringEntityType, StoreObjectType.Table)
                              ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{property.Property.DeclaringEntityType.Name}'.");
            var columnName = property.Property.GetColumnName(storeObject)
                             ?? throw new Exception($"The property '{property.Property.Name}' has no column name.");

            var escapedColumnName = sqlGenerationHelper.DelimitIdentifier(columnName);
            sb.Append(escapedColumnName);
         }

         sb.AppendLine(") DO");
         var sbIndexBeforeUpdate = sb.Length;

         sb.Append("\tUPDATE ");
         var hasPropertiesToUpdate = SqliteUpdateBuilder.GenerateSetAndWhereClause(sb, sqlGenerationHelper, reader, _propertiesToUpdate, _keyProperties);

         if (!hasPropertiesToUpdate)
         {
            sb.Length = sbIndexBeforeUpdate;
            sb.Append("\tNOTHING");
         }

         sb.Append(sqlGenerationHelper.StatementTerminator);

         return sb.ToString();
      }
   }
}