namespace Thinktecture.Extensions;

public static class StringExtensions
{
   public static string WithEnvironmentLineBreaks(this string text)
   {
      text = text.Replace("\r\n", "\n");

      if (Environment.NewLine == "\r\n")
         text = text.Replace("\n", "\r\n");

      return text;
   }
}
