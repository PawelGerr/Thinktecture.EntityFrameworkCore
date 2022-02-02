using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.Internal;

/// <summary>
/// Temp table infos.
/// </summary>
/// <param name="Name">The name of the temp table.</param>
/// <param name="HasOwnedEntities">Indication whether the temp table has owned entities.</param>
/// <param name="EntityType">The entity type of the temp table.</param>
public record TempTableInfo(string Name, bool HasOwnedEntities, IEntityType EntityType);
