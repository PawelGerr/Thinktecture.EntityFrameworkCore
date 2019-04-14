using Microsoft.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext
{
   public class DbContextWithoutSchema : DbContext
   {
      public DbContextWithoutSchema(DbContextOptions<DbContextWithoutSchema> options)
         : base(options)
      {
      }
   }
}
