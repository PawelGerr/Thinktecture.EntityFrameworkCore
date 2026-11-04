using System.Reflection;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// This is an internal API. Represents property access information extracted from an expression.
/// Can be either a direct member access or an <c>EF.Property</c> call.
/// </summary>
public readonly struct PropertyAccessInfo
{
   /// <summary>
   /// The CLR member for direct access, or <c>null</c> for <c>EF.Property</c> calls.
   /// </summary>
   public MemberInfo? Member { get; }

   /// <summary>
   /// The property name — from <see cref="MemberInfo.Name"/> for direct access,
   /// or from the <c>EF.Property</c> string argument.
   /// </summary>
   public string PropertyName { get; }

   /// <summary>
   /// The complex properties between the entity and <see cref="Member"/>, starting at the entity,
   /// e.g. <c>[Boundary]</c> for <c>e => e.Boundary.Upper</c>. Empty for a property of the entity itself.
   /// </summary>
   public IReadOnlyList<MemberInfo> ComplexPath { get; }

   /// <summary>
   /// Initializes a new instance from a direct member access through complex properties.
   /// </summary>
   public PropertyAccessInfo(MemberInfo member, IReadOnlyList<MemberInfo> complexPath)
   {
      ArgumentNullException.ThrowIfNull(member);
      ArgumentNullException.ThrowIfNull(complexPath);

      Member = member;
      PropertyName = member.Name;
      ComplexPath = complexPath;
   }

   /// <summary>
   /// Initializes a new instance from an <c>EF.Property</c> property name.
   /// </summary>
   public PropertyAccessInfo(string propertyName)
   {
      ArgumentNullException.ThrowIfNull(propertyName);

      Member = null;
      PropertyName = propertyName;
      ComplexPath = Array.Empty<MemberInfo>();
   }
}
