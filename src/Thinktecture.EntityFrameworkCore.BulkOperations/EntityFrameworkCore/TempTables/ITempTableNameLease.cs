namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Contains the name of the temp table.
/// The instance of a <see cref="ITempTableNameLease"/> should be disposed of along with the corresponding temp table.
/// </summary>
public interface ITempTableNameLease : IDisposable
{
   /// <summary>
   /// The name of the temp table.
   /// </summary>
   string Name { get; }
}
