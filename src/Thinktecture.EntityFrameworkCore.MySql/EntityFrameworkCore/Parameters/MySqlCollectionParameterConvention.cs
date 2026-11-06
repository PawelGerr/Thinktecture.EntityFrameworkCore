using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.Parameters;

internal sealed class MySqlCollectionParameterConvention : IModelInitializedConvention
{
   public static readonly IModelInitializedConvention Instance = new MySqlCollectionParameterConvention();

   public void ProcessModelInitialized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
   {
      AddScalarCollectionParameter<int>(modelBuilder);
      AddScalarCollectionParameter<int?>(modelBuilder);
      AddScalarCollectionParameter<long>(modelBuilder);
      AddScalarCollectionParameter<long?>(modelBuilder);
      AddScalarCollectionParameter<decimal>(modelBuilder);
      AddScalarCollectionParameter<decimal?>(modelBuilder);
      AddScalarCollectionParameter<double>(modelBuilder);
      AddScalarCollectionParameter<double?>(modelBuilder);
      AddScalarCollectionParameter<float>(modelBuilder);
      AddScalarCollectionParameter<float?>(modelBuilder);
      AddScalarCollectionParameter<short>(modelBuilder);
      AddScalarCollectionParameter<short?>(modelBuilder);
      AddScalarCollectionParameter<byte>(modelBuilder);
      AddScalarCollectionParameter<byte?>(modelBuilder);
      AddScalarCollectionParameter<bool>(modelBuilder);
      AddScalarCollectionParameter<bool?>(modelBuilder);
      AddScalarCollectionParameter<string>(modelBuilder);
      AddScalarCollectionParameter<Guid>(modelBuilder);
      AddScalarCollectionParameter<Guid?>(modelBuilder);
      AddScalarCollectionParameter<DateTime>(modelBuilder);
      AddScalarCollectionParameter<DateTime?>(modelBuilder);
      AddScalarCollectionParameter<DateTimeOffset>(modelBuilder);
      AddScalarCollectionParameter<DateTimeOffset?>(modelBuilder);
      AddScalarCollectionParameter<TimeSpan>(modelBuilder);
      AddScalarCollectionParameter<TimeSpan?>(modelBuilder);
   }

   private static void AddScalarCollectionParameter<T>(IConventionModelBuilder modelBuilder)
   {
      var entityBuilder = modelBuilder.SharedTypeEntity(EntityNameProvider.GetCollectionParameterName(typeof(T), true),
                                                        typeof(ScalarCollectionParameter<T>),
                                                        fromDataAnnotation: true);

      if (entityBuilder is null)
         return;

      entityBuilder.ToTable(typeof(ScalarCollectionParameter<T>).ShortDisplayName(), fromDataAnnotation: true);
      entityBuilder.HasNoKey(fromDataAnnotation: true);
      entityBuilder.ExcludeTableFromMigrations(true, fromDataAnnotation: true);
   }
}
