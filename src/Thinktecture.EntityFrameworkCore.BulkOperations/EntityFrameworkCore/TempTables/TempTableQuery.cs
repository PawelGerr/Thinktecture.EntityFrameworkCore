namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Represents a query pointing to a temp table.
/// </summary>
/// <typeparam name="T">Type of the query item.</typeparam>
public sealed class TempTableQuery<T> : ITempTableQuery<T>
{
   private ITempTableReference? _tempTableReference;
   private IQueryable<T>? _query;

   /// <summary>
   /// The query itself.
   /// </summary>
   public IQueryable<T> Query => _query ?? throw new ObjectDisposedException(nameof(TempTableQuery<T>));

   /// <summary>
   /// The name of the temp table.
   /// </summary>
   public string Name => _tempTableReference?.Name ?? throw new ObjectDisposedException(nameof(TempTableQuery<T>));

   /// <summary>
   /// Gets the number of rows that were inserted into the temp table.
   /// </summary>
   public int NumberOfInsertedRows { get; }

   /// <summary>
   /// Initializes new instance of <see cref="TempTableQuery{T}"/>.
   /// </summary>
   /// <param name="query">Query.</param>
   /// <param name="tempTableReference">Reference to a temp table.</param>
   /// <param name="numberOfInsertedRows">Number of rows inserted into the temp table.</param>
   public TempTableQuery(IQueryable<T> query, ITempTableReference tempTableReference, int numberOfInsertedRows)
   {
      _query = query ?? throw new ArgumentNullException(nameof(query));
      _tempTableReference = tempTableReference ?? throw new ArgumentNullException(nameof(tempTableReference));
      NumberOfInsertedRows = numberOfInsertedRows;
   }

   /// <inheritdoc />
   public void Dispose()
   {
      var tableRef = _tempTableReference;

      if (tableRef == null)
         return;

      tableRef.Dispose();
      _tempTableReference = null;
      _query = null;
   }

   /// <inheritdoc />
   public async ValueTask DisposeAsync()
   {
      var tableRef = _tempTableReference;

      if (tableRef == null)
         return;

      await tableRef.DisposeAsync().ConfigureAwait(false);
      _tempTableReference = null;
      _query = null;
   }
}
