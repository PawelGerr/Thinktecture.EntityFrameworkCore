using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// Entity with provided name and/or type is not part of current <see cref="DbContext"/>.
/// </summary>
public class EntityTypeNotFoundException : ArgumentException
{
   /// <summary>
   /// Initializes new instance of <see cref="EntityTypeNotFoundException"/>.
   /// </summary>
   /// <param name="entityName">Entity name not found in the model of current <see cref="DbContext"/>.</param>
   /// <param name="paramName">The name of the parameter.</param>
   public EntityTypeNotFoundException(
      string entityName,
      [CallerArgumentExpression("entityName")] string? paramName = null)
      : base($"The provided name '{entityName}' is not part of the provided Entity Framework model.", paramName)
   {
   }

   /// <summary>
   /// Initializes new instance of <see cref="EntityTypeNotFoundException"/>.
   /// </summary>
   /// <param name="entityType">The type of the entity not found in the model of current <see cref="DbContext"/>.</param>
   /// <param name="paramName">The name of the parameter.</param>
   public EntityTypeNotFoundException(
      Type entityType,
      [CallerArgumentExpression("entityType")] string? paramName = null)
      : base($"The provided type '{entityType.ShortDisplayName()}' is not part of the provided Entity Framework model.", paramName)
   {
   }

   /// <summary>
   /// Initializes new instance of <see cref="EntityTypeNotFoundException"/>.
   /// </summary>
   /// <param name="entityName">Entity name not found in the model of current <see cref="DbContext"/>.</param>
   /// <param name="entityType">The type of the entity not found in the model of current <see cref="DbContext"/>.</param>
   /// <param name="entityNameParamName">The name of the parameter.</param>
   public EntityTypeNotFoundException(
      string entityName,
      Type entityType,
      [CallerArgumentExpression("entityName")] string? entityNameParamName = null)
      : base($"The provided name '{entityName}' and the type '{entityType.ShortDisplayName()}' were not part of the provided Entity Framework model.", entityNameParamName)
   {
   }
}
