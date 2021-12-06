namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// Migrates the database.
/// </summary>
public interface IMigrationExecutionStrategy
{
   /// <summary>
   /// Migrates the database.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   void Migrate(DbContext ctx);
}