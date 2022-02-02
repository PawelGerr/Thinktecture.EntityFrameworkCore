namespace Thinktecture.Internal;

/// <summary>
/// Temp table infos.
/// </summary>
public class TempTableInfoExpression : NonEvaluatableConstantExpression<TempTableInfo>
{
   /// <inheritdoc />
   public TempTableInfoExpression(TempTableInfo value)
      : base(value)
   {
   }

   /// <inheritdoc />
   public override bool Equals(TempTableInfo? otherTempTableName)
   {
      return Value.Equals(otherTempTableName);
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      return HashCode.Combine(Value);
   }
}
