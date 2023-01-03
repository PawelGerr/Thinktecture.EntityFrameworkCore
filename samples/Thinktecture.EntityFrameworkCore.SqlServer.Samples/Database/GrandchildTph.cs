namespace Thinktecture.Database;

public class GrandchildTph : ChildTph
{
   public const string DISCRIMINATOR = "grandchild";

   public int GrandChildProp { get; set; }

   public GrandchildTph()
      : base(DISCRIMINATOR)
   {
   }
}
