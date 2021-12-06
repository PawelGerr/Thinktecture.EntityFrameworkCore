using Microsoft.EntityFrameworkCore;

namespace Thinktecture.EntityFrameworkCore;

internal sealed class EnsureCreatedMigrationExecutionStrategy : IMigrationExecutionStrategy
{
   public void Migrate(DbContext ctx)
   {
      ctx.Database.EnsureCreated();
   }
}