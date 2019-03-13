using System;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore
{
   public class DbContextSchema : IDbContextSchema
   {
      public string Schema { get; }

      public DbContextSchema([NotNull] string schema)
      {
         Schema = schema ?? throw new ArgumentNullException(nameof(schema));
      }
   }
}
