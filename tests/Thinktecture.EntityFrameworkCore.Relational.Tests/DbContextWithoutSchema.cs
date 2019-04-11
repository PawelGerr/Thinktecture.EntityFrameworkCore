using Microsoft.EntityFrameworkCore;

namespace Thinktecture
{
   public class DbContextWithoutSchema : DbContext
   {
      public DbContextWithoutSchema(DbContextOptions<DbContextWithoutSchema> options)
         : base(options)
      {
      }
   }
}
