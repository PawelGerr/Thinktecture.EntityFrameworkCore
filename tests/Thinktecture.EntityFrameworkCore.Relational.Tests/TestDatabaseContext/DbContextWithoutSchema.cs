using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext
{
   public class DbContextWithoutSchema : DbContext
   {
      public DbContextWithoutSchema([NotNull] DbContextOptions<DbContextWithoutSchema> options)
         : base(options)
      {
      }
   }
}
