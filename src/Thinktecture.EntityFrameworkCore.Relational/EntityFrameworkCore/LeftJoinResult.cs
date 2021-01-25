using System.Diagnostics.CodeAnalysis;

namespace Thinktecture.EntityFrameworkCore
{
   /// <summary>
   /// Result of a LEFT JOIN.
   /// </summary>
   /// <typeparam name="TLeft">Type of the entity on the left side of the JOIN.</typeparam>
   /// <typeparam name="TRight">Type of the entity  on the right side of the JOIN.</typeparam>
   public sealed class LeftJoinResult<TLeft, TRight>
      where TLeft : notnull
   {
      /// <summary>
      /// Entity on the left side of the JOIN.
      /// </summary>
      [DisallowNull]
      public TLeft Left { get; init; }

      /// <summary>
      /// Entity  on the right side of the JOIN.
      /// </summary>
      [MaybeNull, AllowNull]
      public TRight Right { get; init; }

#nullable disable
      internal LeftJoinResult()
      {
      }
#nullable enable
   }
}
