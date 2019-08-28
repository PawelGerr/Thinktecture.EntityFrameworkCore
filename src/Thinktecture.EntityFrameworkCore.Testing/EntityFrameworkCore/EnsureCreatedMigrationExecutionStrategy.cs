using Microsoft.EntityFrameworkCore;

namespace Thinktecture.EntityFrameworkCore
{
   internal class EnsureCreatedMigrationExecutionStrategy : IMigrationExecutionStrategy
   {
      public void Migrate(DbContext ctx)
      {
         ctx.Database.EnsureCreated();
      }
   }
}
