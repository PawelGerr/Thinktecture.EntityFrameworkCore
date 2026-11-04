namespace Thinktecture.TestDatabaseContext;

// EF Core cannot bind a nested complex type to a constructor parameter, so the member uses an init accessor.
public readonly record struct TextEnvelope
{
   public TextPair Texts { get; init; }
}
