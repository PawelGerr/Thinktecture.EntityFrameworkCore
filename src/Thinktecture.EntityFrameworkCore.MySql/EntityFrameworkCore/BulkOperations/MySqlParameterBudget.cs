using System.Runtime.CompilerServices;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Validates the maximum number of parameters of one multi-row INSERT statement.
/// </summary>
internal static class MySqlParameterBudget
{
   // MySQL accepts at most 65,535 placeholders in a prepared statement.
   private const int _MAXIMUM = 65_535;

   public static int Validate(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
   {
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _MAXIMUM, paramName);
      return value;
   }
}
