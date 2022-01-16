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
      AddTempTable<long>(modelBuilder);
      AddTempTable<DateTime>(modelBuilder);
      AddTempTable<Guid>(modelBuilder);
      AddTempTable<bool>(modelBuilder);
      AddTempTable<byte>(modelBuilder);
      AddTempTable<double>(modelBuilder);
      AddTempTable<DateTimeOffset>(modelBuilder);
      AddTempTable<short>(modelBuilder);
      AddTempTable<float>(modelBuilder);
      AddTempTable<decimal>(modelBuilder);
      AddTempTable<TimeSpan>(modelBuilder);
      AddTempTable<string>(modelBuilder);
   }

   private static void AddTempTable<TColumn1>(IConventionModelBuilder modelBuilder)
   {
      var type = typeof(TempTable<TColumn1>);
      var builder = modelBuilder.SharedTypeEntity(EntityNameProvider.GetTempTableName(type), type, fromDataAnnotation: true);

      if (builder is null)
         return;

      builder.ToTable($"#{type.ShortDisplayName()}");
      builder.HasNoKey();
      builder.ExcludeTableFromMigrations(true);
   }
}
