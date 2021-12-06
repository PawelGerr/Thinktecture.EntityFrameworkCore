using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Sets default database schema.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DefaultSchemaModelCustomizer<TModelCustomizer> : IModelCustomizer
   where TModelCustomizer : class, IModelCustomizer
{
   private readonly TModelCustomizer _modelCustomizer;

   /// <summary>
   /// Initializes new instance <see cref="DefaultSchemaModelCustomizer{TModelCustomizer}"/>.
   /// </summary>
   /// <param name="modelCustomizer">Inner model customizer.</param>
   public DefaultSchemaModelCustomizer(TModelCustomizer modelCustomizer)
   {
      _modelCustomizer = modelCustomizer ?? throw new ArgumentNullException(nameof(modelCustomizer));
   }

   /// <inheritdoc />
   public void Customize(ModelBuilder modelBuilder, DbContext context)
   {
      if (modelBuilder == null)
         throw new ArgumentNullException(nameof(modelBuilder));
      if (context == null)
         throw new ArgumentNullException(nameof(context));

      _modelCustomizer.Customize(modelBuilder, context);

      // ReSharper disable once SuspiciousTypeConversion.Global
      if (context is IDbDefaultSchema { Schema: { } } schema)
         modelBuilder.HasDefaultSchema(schema.Schema);
   }
}
