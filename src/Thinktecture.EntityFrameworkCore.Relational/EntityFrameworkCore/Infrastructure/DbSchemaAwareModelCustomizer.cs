using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Set database schema on entities without schema.
   /// </summary>
   public class DbSchemaAwareModelCustomizer : RelationalModelCustomizer
   {
      /// <inheritdoc />
      public DbSchemaAwareModelCustomizer([NotNull] ModelCustomizerDependencies dependencies)
         : base(dependencies)
      {
      }

      /// <inheritdoc />
      public override void Customize(ModelBuilder modelBuilder, DbContext context)
      {
         base.Customize(modelBuilder, context);

         if (context is IDbContextSchema schema && schema.Schema != null)
            modelBuilder.SetSchema(schema.Schema, entityType => entityType.Schema == null);
      }
   }
}
