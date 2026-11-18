using System.Globalization;

namespace Thinktecture.EntityFrameworkCore.Internal;

/// <summary>
/// Formats a <see cref="TimeSpan" /> as the text of a MySQL <c>TIME</c> value.
/// </summary>
internal static class MySqlTimeFormatter
{
   /// <summary>
   /// Formats the value as <c>[-]HH:mm:ss.ffffff</c> with the total hours (at least two digits) and six fraction digits.
   /// MySQL takes hours beyond 23 (up to 838), unlike the .NET constant format "d.hh:mm:ss", and rounds the fraction to the precision of the column.
   /// </summary>
   public static string Format(TimeSpan value)
   {
      // The unsigned negation also works for TimeSpan.MinValue, for which Math.Abs throws an OverflowException.
      var ticks = value.Ticks < 0 ? unchecked((ulong)-value.Ticks) : (ulong)value.Ticks;

      return String.Create(CultureInfo.InvariantCulture,
                           $"{(value.Ticks < 0 ? "-" : "")}{ticks / TimeSpan.TicksPerHour:00}:{ticks / TimeSpan.TicksPerMinute % 60:00}:{ticks / TimeSpan.TicksPerSecond % 60:00}.{ticks % TimeSpan.TicksPerSecond / 10:000000}");
   }
}
