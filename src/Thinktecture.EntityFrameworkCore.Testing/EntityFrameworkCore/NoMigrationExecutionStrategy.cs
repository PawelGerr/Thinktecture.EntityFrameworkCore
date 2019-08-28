using Microsoft.EntityFrameworkCore;

namespace Thinktecture.EntityFrameworkCore
{
   internal class NoMigrationExecutionStrategy : IMigrationExecutionStrategy
   {
      public void Migrate(DbContext ctx)
      {
      }
   }
}
