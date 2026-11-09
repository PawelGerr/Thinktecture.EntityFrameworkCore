namespace Thinktecture.TestDatabaseContext;

public class OuterWithStructs
{
   public int Y { get; set; }
   public TimestampPair Pair { get; set; }
   public TaggedStruct TaggedStruct { get; set; }
   public TimestampPair? OptionalPair { get; set; }
}
