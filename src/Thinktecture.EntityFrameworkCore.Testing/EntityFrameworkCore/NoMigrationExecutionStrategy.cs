namespace Thinktecture.EntityFrameworkCore;

internal sealed class NoMigrationExecutionStrategy : IMigrationExecutionStrategy
{
   public void Migrate(DbContext ctx)
   {
   }
}