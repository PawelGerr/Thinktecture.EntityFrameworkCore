using System.Diagnostics.CodeAnalysis;

namespace Thinktecture.EntityFrameworkCore
{
   /// <summary>
   /// Result of a LEFT JOIN.
   /// </summary>
   /// <typeparam name="TLeft">Type of the entity on the left side of the JOIN.</typeparam>
   /// <typeparam name="TRight">Type of the entity  on the right side of the JOIN.</typeparam>
   public class LeftJoinResult<TLeft, TRight>
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

   /// <summary>
   /// Result of a LEFT JOIN.
   /// </summary>
   /// <typeparam name="TLeft">Type of the entity on the left side of the JOIN.</typeparam>
   /// <typeparam name="TRight">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight2">Type of the entity on the right side of the JOIN.</typeparam>
   public class LeftJoinResult<TLeft, TRight, TRight2> : LeftJoinResult<TLeft, TRight>
      where TLeft : notnull
   {
      /// <summary>
      /// Entity  on the right side of the JOIN.
      /// </summary>
      [MaybeNull, AllowNull]
      public TRight2 Right2 { get; init; }

#nullable disable
      internal LeftJoinResult()
      {
      }
#nullable enable
   }

   /// <summary>
   /// Result of a LEFT JOIN.
   /// </summary>
   /// <typeparam name="TLeft">Type of the entity on the left side of the JOIN.</typeparam>
   /// <typeparam name="TRight">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight2">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight3">Type of the entity on the right side of the JOIN.</typeparam>
   public class LeftJoinResult<TLeft, TRight, TRight2, TRight3> : LeftJoinResult<TLeft, TRight, TRight2>
      where TLeft : notnull
   {
      /// <summary>
      /// Entity  on the right side of the JOIN.
      /// </summary>
      [MaybeNull, AllowNull]
      public TRight3 Right3 { get; init; }

#nullable disable
      internal LeftJoinResult()
      {
      }
#nullable enable
   }

   /// <summary>
   /// Result of a LEFT JOIN.
   /// </summary>
   /// <typeparam name="TLeft">Type of the entity on the left side of the JOIN.</typeparam>
   /// <typeparam name="TRight">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight2">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight3">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight4">Type of the entity on the right side of the JOIN.</typeparam>
   public class LeftJoinResult<TLeft, TRight, TRight2, TRight3, TRight4> : LeftJoinResult<TLeft, TRight, TRight2, TRight3>
      where TLeft : notnull
   {
      /// <summary>
      /// Entity  on the right side of the JOIN.
      /// </summary>
      [MaybeNull, AllowNull]
      public TRight4 Right4 { get; init; }

#nullable disable
      internal LeftJoinResult()
      {
      }
#nullable enable
   }

   /// <summary>
   /// Result of a LEFT JOIN.
   /// </summary>
   /// <typeparam name="TLeft">Type of the entity on the left side of the JOIN.</typeparam>
   /// <typeparam name="TRight">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight2">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight3">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight4">Type of the entity on the right side of the JOIN.</typeparam>
   /// <typeparam name="TRight5">Type of the entity on the right side of the JOIN.</typeparam>
   public class LeftJoinResult<TLeft, TRight, TRight2, TRight3, TRight4, TRight5> : LeftJoinResult<TLeft, TRight, TRight2, TRight3, TRight4>
      where TLeft : notnull
   {
      /// <summary>
      /// Entity  on the right side of the JOIN.
      /// </summary>
      [MaybeNull, AllowNull]
      public TRight5 Right5 { get; init; }

#nullable disable
      internal LeftJoinResult()
      {
      }
#nullable enable
   }
}
