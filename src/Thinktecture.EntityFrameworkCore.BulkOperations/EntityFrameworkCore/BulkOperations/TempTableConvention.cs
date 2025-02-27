using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Configures some temp tables for primitive types.
/// </summary>
public class TempTableConvention : IModelInitializedConvention
{
   /// <summary>
   /// Singleton.
   /// </summary>
   public static readonly IModelInitializedConvention Instance = new TempTableConvention();

   /// <inheritdoc />
   public void ProcessModelInitialized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
   {
      AddTempTable<int>(modelBuilder);
      AddTempTable<int?>(modelBuilder);
      AddTempTable<long>(modelBuilder);
      AddTempTable<long?>(modelBuilder);
      AddTempTable<DateTime>(modelBuilder);
      AddTempTable<DateTime?>(modelBuilder);
      AddTempTable<DateOnly>(modelBuilder);
      AddTempTable<DateOnly?>(modelBuilder);
      AddTempTable<TimeOnly>(modelBuilder);
      AddTempTable<TimeOnly?>(modelBuilder);
      AddTempTable<Guid>(modelBuilder);
      AddTempTable<Guid?>(modelBuilder);
      AddTempTable<bool>(modelBuilder);
      AddTempTable<bool?>(modelBuilder);
      AddTempTable<byte>(modelBuilder);
      AddTempTable<byte?>(modelBuilder);
      AddTempTable<double>(modelBuilder);
      AddTempTable<double?>(modelBuilder);
      AddTempTable<DateTimeOffset>(modelBuilder);
      AddTempTable<DateTimeOffset?>(modelBuilder);
      AddTempTable<short>(modelBuilder);
      AddTempTable<short?>(modelBuilder);
      AddTempTable<float>(modelBuilder);
      AddTempTable<float?>(modelBuilder);
      AddTempTable<decimal>(modelBuilder, (38, 18));
      AddTempTable<decimal?>(modelBuilder, (38, 18));
      AddTempTable<TimeSpan>(modelBuilder);
      AddTempTable<TimeSpan?>(modelBuilder);
      AddTempTable<string>(modelBuilder);
   }

   private static void AddTempTable<TColumn1>(
      IConventionModelBuilder modelBuilder,
      (int Precision, int Scale)? precisionAndScale = null)
   {
      var type = typeof(TempTable<TColumn1>);
      var builder = modelBuilder.SharedTypeEntity(EntityNameProvider.GetTempTableName(type), type, fromDataAnnotation: true);

      if (builder is null)
         return;

      if (precisionAndScale is not null)
         SetScaleAndPrecision<TColumn1>(builder, precisionAndScale.Value);

      builder.ToTable($"#{type.ShortDisplayName()}");
      builder.HasNoKey();
      builder.ExcludeTableFromMigrations(true);
   }

   private static void SetScaleAndPrecision<TColumn1>(
      IConventionEntityTypeBuilder builder,
      (int Precision, int Scale) precisionAndScale)
   {
      var propertyBuilder = builder.Property(typeof(TColumn1), nameof(TempTable<TColumn1>.Column1), fromDataAnnotation: true);

      if (propertyBuilder is null)
         return;

      propertyBuilder.HasPrecision(precisionAndScale.Precision, true);
      propertyBuilder.HasScale(precisionAndScale.Scale, true);
   }
}
