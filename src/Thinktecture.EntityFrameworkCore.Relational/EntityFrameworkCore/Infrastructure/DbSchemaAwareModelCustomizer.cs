using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Set database schema on entities without schema.
   /// </summary>
   // ReSharper disable once ClassNeverInstantiated.Global
   public class DbSchemaAwareModelCustomizer<TModelCustomizer> : IModelCustomizer
      where TModelCustomizer : class, IModelCustomizer
   {
      private readonly TModelCustomizer _modelCustomizer;

      /// <summary>
      /// Initializes new instance <see cref="DbSchemaAwareModelCustomizer{TIModelCustomizer}"/>.
      /// </summary>
      /// <param name="modelCustomizer">Inner model customizer.</param>
      public DbSchemaAwareModelCustomizer([NotNull] TModelCustomizer modelCustomizer)
      {
         _modelCustomizer = modelCustomizer ?? throw new ArgumentNullException(nameof(modelCustomizer));
      }

      /// <inheritdoc />
      public void Customize(ModelBuilder modelBuilder, DbContext context)
      {
         _modelCustomizer.Customize(modelBuilder, context);

         if (context is IDbDefaultSchema schema && schema.Schema != null)
            modelBuilder.HasDefaultSchema(schema.Schema);
      }
   }
}
