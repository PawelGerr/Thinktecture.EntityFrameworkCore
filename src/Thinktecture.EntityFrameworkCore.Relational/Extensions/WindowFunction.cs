using System.Text.RegularExpressions;

namespace Thinktecture;

/// <summary>
/// Represents a window function name.
/// </summary>
public abstract partial class WindowFunction
{
   private const string _REGEX_PATTERN = "^[A-Z_]+$";
   private const RegexOptions _REGEX_OPTIONS = RegexOptions.IgnoreCase | RegexOptions.Compiled;

   [GeneratedRegex(_REGEX_PATTERN, _REGEX_OPTIONS)]
   private static partial Regex ValidateName();

   /// <summary>
   /// Name of the function.
   /// </summary>
   public string Name { get; }

   /// <summary>
   /// Return type of the function.
   /// </summary>
   public Type ReturnType { get; }

   /// <summary>
   /// Indication whether to use '*' when no arguments are provided.
   /// </summary>
   public bool UseAsteriskWhenNoArguments { get; }

   /// <summary>
   /// Initializes a new instance of <see cref="WindowFunction"/>.
   /// </summary>
   /// <param name="name">The name of the window function.</param>
   /// <param name="returnType">Return type of the function.</param>
   /// <param name="useAsteriskWhenNoArguments">Indication whether to use '*' when no arguments are provided.</param>
   protected WindowFunction(string name, Type returnType, bool useAsteriskWhenNoArguments)
   {
      Name = EnsureValidName(name);
      ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
      UseAsteriskWhenNoArguments = useAsteriskWhenNoArguments;
   }

   private static string EnsureValidName(string name)
   {
      if (String.IsNullOrWhiteSpace(name))
         throw new ArgumentException("Name must not be empty.");

      name = name.Trim();

      if (!ValidateName().IsMatch(name))
         throw new ArgumentException("The name must consist of characters A-Z and may contain an underscore.");

      return name;
   }

   /// <summary>
   /// Deconstruction of the window function.
   /// </summary>
   /// <param name="name">The name of the function.</param>
   /// <param name="returnType">The return type of the function.</param>
   /// <param name="useAsteriskWhenNoArguments">Indication whether to use '*' when no arguments are provided.</param>
   public void Deconstruct(out string name, out Type returnType, out bool useAsteriskWhenNoArguments)
   {
      name = Name;
      returnType = ReturnType;
      useAsteriskWhenNoArguments = UseAsteriskWhenNoArguments;
   }
}

/// <summary>
/// Represents a window function name.
/// </summary>
public sealed class WindowFunction<TResult> : WindowFunction, IEquatable<WindowFunction<TResult>>
{
   /// <summary>
   /// Initializes a new instance of <see cref="WindowFunction{TResult}"/>.
   /// </summary>
   /// <param name="name">The name of the window function</param>
   /// <param name="useAsteriskWhenNoArguments">Indication whether to use '*' when no arguments are provided.</param>
   public WindowFunction(
      string name,
      bool useAsteriskWhenNoArguments = false)
      : base(name, typeof(TResult), useAsteriskWhenNoArguments)
   {
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return Equals(obj as WindowFunction<TResult>);
   }

   /// <inheritdoc />
   public bool Equals(WindowFunction<TResult>? other)
   {
      if (ReferenceEquals(null, other))
         return false;
      if (ReferenceEquals(this, other))
         return true;

      return Name == other.Name;
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      return Name.GetHashCode();
   }

   /// <inheritdoc />
   public override string ToString()
   {
      return Name;
   }
}
