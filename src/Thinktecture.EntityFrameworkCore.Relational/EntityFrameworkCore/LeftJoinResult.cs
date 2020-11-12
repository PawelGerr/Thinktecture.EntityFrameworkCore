namespace Thinktecture.EntityFrameworkCore
{
#pragma warning disable CS1572,CS1573,CS1591
   /// <summary>
   /// Result of a LEFT JOIN.
   /// </summary>
   /// <param name="Left">Entity on the left side of the JOIN.</param>
   /// <param name="Right">Entity on the right side of the JOIN.</param>
   /// <typeparam name="TLeft">Type of the entity on the left side of the JOIN.</typeparam>
   /// <typeparam name="TRight">Type of the entity  on the right side of the JOIN.</typeparam>
   public sealed record LeftJoinResult<TLeft, TRight>(
      TLeft Left,
      TRight? Right)
      where TLeft : notnull;
#pragma warning restore CS1572,CS1573,CS1591
}
