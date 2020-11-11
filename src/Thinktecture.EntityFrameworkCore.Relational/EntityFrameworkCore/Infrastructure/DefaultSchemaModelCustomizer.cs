using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
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

         if (context is IDbDefaultSchema schema && schema.Schema != null)
         {
            modelBuilder.HasDefaultSchema(schema.Schema);

            // fix for regression: https://github.com/dotnet/efcore/issues/23274
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
               if (entityType.GetViewName() is not null &&
                   entityType.FindAnnotation(RelationalAnnotationNames.ViewSchema) is { Value: null })
               {
                  entityType.SetViewSchema(schema.Schema);
               }
            }
         }
      }
   }
}
