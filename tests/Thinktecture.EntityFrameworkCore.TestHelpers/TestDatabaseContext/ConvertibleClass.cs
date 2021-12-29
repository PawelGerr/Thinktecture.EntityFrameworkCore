namespace Thinktecture.TestDatabaseContext;

public class ConvertibleClass
{
   public int Key { get; set; }

   public ConvertibleClass(int key)
   {
      Key = key;
   }

   public static implicit operator int(ConvertibleClass convertibleClass)
   {
      return convertibleClass.Key;
   }
}
