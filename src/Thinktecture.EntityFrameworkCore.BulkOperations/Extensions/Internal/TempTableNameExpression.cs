using System;

namespace Thinktecture.Internal
{
   /// <summary>
   /// The name of the temp table.
   /// </summary>
   public class TempTableNameExpression : NonEvaluatableConstantExpression<string>
   {
      /// <inheritdoc />
      public TempTableNameExpression(string value)
         : base(value)
      {
      }

      /// <inheritdoc />
      public override bool Equals(string? otherTempTableName)
      {
         return Value.Equals(otherTempTableName);
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         return HashCode.Combine(Value);
      }
   }
}
