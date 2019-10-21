using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore
{
   /// <summary>
   /// A few implementations of <see cref="IMigrationExecutionStrategy"/>.
   /// </summary>
   public static class MigrationExecutionStrategies
   {
      /// <summary>
      /// The database will not be migrated.
      /// </summary>
      public static readonly IMigrationExecutionStrategy NoMigration = new NoMigrationExecutionStrategy();

      /// <summary>
      /// The database will be migrated using <see cref="RelationalDatabaseFacadeExtensions.Migrate"/>.
      /// </summary>
      public static readonly IMigrationExecutionStrategy Migrations = new MigrationExecutionStrategy();

      /// <summary>
      /// The database will be migrated using <see cref="DatabaseFacade.EnsureCreated"/>.
      /// </summary>
      public static readonly IMigrationExecutionStrategy EnsureCreated = new EnsureCreatedMigrationExecutionStrategy();
   }
}
