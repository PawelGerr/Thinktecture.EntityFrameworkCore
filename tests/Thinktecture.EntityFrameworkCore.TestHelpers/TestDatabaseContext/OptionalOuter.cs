namespace Thinktecture.TestDatabaseContext;

public class OptionalOuter
{
   public int Y { get; set; }
   public InnerValue Inner { get; set; } = new();
}
