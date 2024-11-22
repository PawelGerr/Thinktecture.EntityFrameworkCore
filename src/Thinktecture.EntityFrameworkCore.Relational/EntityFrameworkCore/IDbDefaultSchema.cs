namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// Represents a DB schema-containing component.
/// </summary>
public interface IDbDefaultSchema
{
   /// <summary>
   /// Database schema.
   /// </summary>
   string? Schema { get; }
}