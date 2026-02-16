using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.Parameters;

internal class NpgsqlCollectionParameterConvention : IModelInitializedConvention
{
   public static readonly IModelInitializedConvention Instance = new NpgsqlCollectionParameterConvention();

   public void ProcessModelInitialized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
   {
      AddScalarCollectionParameter<int>(modelBuilder);
      AddScalarCollectionParameter<int?>(modelBuilder);
      AddScalarCollectionParameter<long>(modelBuilder);
      AddScalarCollectionParameter<long?>(modelBuilder);
      AddScalarCollectionParameter<DateTime>(modelBuilder);
      AddScalarCollectionParameter<DateTime?>(modelBuilder);
      AddScalarCollectionParameter<Guid>(modelBuilder);
      AddScalarCollectionParameter<Guid?>(modelBuilder);
      AddScalarCollectionParameter<bool>(modelBuilder);
      AddScalarCollectionParameter<bool?>(modelBuilder);
      AddScalarCollectionParameter<byte>(modelBuilder);
      AddScalarCollectionParameter<byte?>(modelBuilder);
      AddScalarCollectionParameter<double>(modelBuilder);
      AddScalarCollectionParameter<double?>(modelBuilder);
      AddScalarCollectionParameter<DateTimeOffset>(modelBuilder);
      AddScalarCollectionParameter<DateTimeOffset?>(modelBuilder);
      AddScalarCollectionParameter<short>(modelBuilder);
      AddScalarCollectionParameter<short?>(modelBuilder);
      AddScalarCollectionParameter<float>(modelBuilder);
      AddScalarCollectionParameter<float?>(modelBuilder);
      AddScalarCollectionParameter<decimal>(modelBuilder);
      AddScalarCollectionParameter<decimal?>(modelBuilder);
      AddScalarCollectionParameter<TimeSpan>(modelBuilder);
      AddScalarCollectionParameter<TimeSpan?>(modelBuilder);
      AddScalarCollectionParameter<string>(modelBuilder);
   }

   private static void AddScalarCollectionParameter<TColumn1>(
      IConventionModelBuilder modelBuilder,
      (int Precision, int Scale)? precisionAndScale = null)
   {
      var builder = modelBuilder.SharedTypeEntity(EntityNameProvider.GetCollectionParameterName(typeof(TColumn1), true),
                                                  typeof(ScalarCollectionParameter<TColumn1>),
                                                  fromDataAnnotation: true);

      if (builder is null)
         return;

      if (precisionAndScale is not null)
         SetScaleAndPrecision<TColumn1>(builder, precisionAndScale.Value);

      builder.ToTable(typeof(ScalarCollectionParameter<TColumn1>).ShortDisplayName());
      builder.HasNoKey();
      builder.ExcludeTableFromMigrations(true);
   }

   private static void SetScaleAndPrecision<TColumn1>(
      IConventionEntityTypeBuilder builder,
      (int Precision, int Scale) precisionAndScale)
   {
      var propertyBuilder = builder.Property(typeof(TColumn1), nameof(ScalarCollectionParameter<TColumn1>.Value), fromDataAnnotation: true);

      if (propertyBuilder is null)
         return;

      propertyBuilder.HasPrecision(precisionAndScale.Precision, fromDataAnnotation: true);
      propertyBuilder.HasScale(precisionAndScale.Scale, fromDataAnnotation: true);
   }
}
