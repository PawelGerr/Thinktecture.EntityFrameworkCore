namespace Thinktecture.EntityFrameworkCore.Parameters;

/// <summary>
/// Represents a collection of items with 1 column/property.
/// </summary>
/// <param name="Value">Value of the column.</param>
/// <typeparam name="T">Type of the column.</typeparam>
public record ScalarCollectionParameter<T>(T Value);
