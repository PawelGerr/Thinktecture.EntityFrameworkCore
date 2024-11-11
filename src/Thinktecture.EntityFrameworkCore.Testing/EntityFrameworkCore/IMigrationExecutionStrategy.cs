using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// Migrates the database.
/// </summary>
public interface IMigrationExecutionStrategy
{
   /// <summary>
   /// The database will not be migrated.
   /// </summary>
   public static readonly IMigrationExecutionStrategy NoMigration = new NoMigrationExecutionStrategy();

   /// <summary>
   /// The database will be migrated using <see cref="RelationalDatabaseFacadeExtensions.Migrate(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade)"/>.
   /// </summary>
   public static readonly IMigrationExecutionStrategy Migrations = new MigrationExecutionStrategy();

   /// <summary>
   /// The database will be migrated using <see cref="DatabaseFacade.EnsureCreated"/>.
   /// </summary>
   public static readonly IMigrationExecutionStrategy EnsureCreated = new EnsureCreatedMigrationExecutionStrategy();

   /// <summary>
   /// Migrates the database.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   void Migrate(DbContext ctx);
}
