using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture
{
   public class DbContextWithSchema : DbContext, IDbContextSchema
   {
      /// <inheritdoc />
      public string Schema { get; }

      public DbContextWithSchema(DbContextOptions<DbContextWithSchema> options, string schema)
         : base(options)
      {
         Schema = schema;
      }
   }
}
