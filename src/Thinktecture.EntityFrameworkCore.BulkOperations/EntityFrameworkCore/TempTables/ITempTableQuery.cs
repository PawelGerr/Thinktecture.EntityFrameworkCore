namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Represents a query pointing to a temp table.
/// Disposal of this query will delete the corresponding temp table.
/// </summary>
/// <typeparam name="T">Type of the query item.</typeparam>
public interface ITempTableQuery<out T> : IAsyncDisposable, IDisposable
{
   /// <summary>
   /// The query itself.
   /// </summary>
   IQueryable<T> Query { get; }

   /// <summary>
   /// The name of the temp table.
   /// </summary>
   string Name { get; }
}
