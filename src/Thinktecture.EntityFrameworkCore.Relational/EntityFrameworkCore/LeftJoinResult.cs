using System.Diagnostics.CodeAnalysis;

namespace Thinktecture.EntityFrameworkCore
{
   /// <summary>
   /// Result of a LEFT JOIN.
   /// </summary>
   /// <typeparam name="TLeft">Type of the entity on the left side of the JOIN.</typeparam>
   /// <typeparam name="TRight">Type of the entity  on the right side of the JOIN.</typeparam>
   public sealed class LeftJoinResult<TLeft, TRight>
   {
      /// <summary>
      /// Entity on the left side of the JOIN.
      /// </summary>
      [NotNull, DisallowNull]
      public TLeft Left { get; set; }

      /// <summary>
      /// Entity  on the right side of the JOIN.
      /// </summary>
      [MaybeNull, AllowNull]
      public TRight Right { get; set; }

#nullable disable
      internal LeftJoinResult()
      {
      }
#nullable enable
   }
}
