namespace Thinktecture.Database;

public class ChildTph : ParentTph
{
   public const string DISCRIMINATOR = "child";

   public int ChildProp { get; set; }

   public ChildTph()
      : base(DISCRIMINATOR)
   {
   }

   protected ChildTph(string discriminator)
      : base(discriminator)
   {
   }
}
