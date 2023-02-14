using Thinktecture.EntityFrameworkCore;

namespace Thinktecture;

public class OneTimeMigrationStrategy : IMigrationExecutionStrategy
{
   private bool _isMigrated;

   public void Migrate(DbContext ctx)
   {
      if (_isMigrated)
         return;

      ctx.Database.Migrate();
      _isMigrated = true;
   }
}
