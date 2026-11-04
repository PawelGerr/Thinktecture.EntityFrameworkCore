namespace Thinktecture.TestDatabaseContext;

// EF Core cannot bind a nested complex type to a constructor parameter, so the members use init accessors.
public readonly record struct MessageEnvelope
{
   public TimestampPair Key { get; init; }
   public long Sequence { get; init; }
}
