namespace Thinktecture.Database;

public class ParentTph
{
   public const string DISCRIMINATOR = "parent";

   public Guid Id { get; set; }

   public int ParentProp { get; set; }

   public string Discriminator { get; }

   public ParentTph()
      : this(DISCRIMINATOR)
   {
   }

   protected ParentTph(string discriminator)
   {
      Discriminator = discriminator;
   }
}
