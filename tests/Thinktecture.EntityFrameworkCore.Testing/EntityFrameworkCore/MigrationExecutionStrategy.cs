namespace Thinktecture.EntityFrameworkCore;

internal sealed class MigrationExecutionStrategy : IMigrationExecutionStrategy
{
   public void Migrate(DbContext ctx)
   {
      ctx.Database.Migrate();
   }
}