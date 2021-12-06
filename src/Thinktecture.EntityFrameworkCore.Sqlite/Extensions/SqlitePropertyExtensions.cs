using System;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="IProperty"/>.
/// </summary>
public static class SqlitePropertyExtensions
{
   internal static bool IsAutoIncrement(this IProperty property)
   {
      if (property == null)
         throw new ArgumentNullException(nameof(property));

      return property.ValueGenerated == ValueGenerated.OnAdd
             && (property.ClrType == typeof(int) || property.ClrType == typeof(int?))
             && property.FindTypeMapping()?.Converter == null
             && property.GetAfterSaveBehavior() != PropertySaveBehavior.Save;
   }
}