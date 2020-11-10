namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Provides the database name of the provided
   /// </summary>
   public interface ITenantDatabaseProvider
   {
      /// <summary>
      /// Gets the current tenant.
      /// </summary>
      string? Tenant { get; }

      /// <summary>
      /// Gets the database name for provided <paramref name="schema"/> and <paramref name="table"/>.
      /// </summary>
      /// <param name="schema">Schema of the table.</param>
      /// <param name="table">The table name.</param>
      /// <returns>The database name.</returns>
      string? GetDatabaseName(string? schema, string table);
   }
}
