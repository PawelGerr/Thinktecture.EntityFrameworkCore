using System;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore
{
   /// <summary>
   /// DB schema.
   /// </summary>
   public class DbContextSchema : IDbContextSchema
   {
      /// <summary>
      /// Database schema
      /// </summary>
      public string Schema { get; }

      /// <summary>
      /// Initializes new instance of <see cref="DbContextSchema"/>.
      /// </summary>
      /// <param name="schema"></param>
      public DbContextSchema([NotNull] string schema)
      {
         Schema = schema ?? throw new ArgumentNullException(nameof(schema));
      }
   }
}
