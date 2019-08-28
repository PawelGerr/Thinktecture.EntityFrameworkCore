using Microsoft.EntityFrameworkCore;

namespace Thinktecture.EntityFrameworkCore
{
   internal class MigrationExecutionStrategy : IMigrationExecutionStrategy
   {
      public void Migrate(DbContext ctx)
      {
         ctx.Database.Migrate();
      }
   }
}
