namespace Thinktecture.TestDatabaseContext;

public class BoundaryValueObject
{
   public int Upper { get; }
   public int Lower { get; }

   public BoundaryValueObject(int upper, int lower)
   {
      Upper = upper;
      Lower = lower;
   }
}
