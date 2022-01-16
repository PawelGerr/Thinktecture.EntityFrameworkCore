namespace Thinktecture.TestDatabaseContext;

public class ConvertibleClass : IEquatable<ConvertibleClass>
{
   public int Key { get; }

   public ConvertibleClass(int key)
   {
      Key = key;
   }

   public static implicit operator int(ConvertibleClass convertibleClass)
   {
      return convertibleClass.Key;
   }

   public bool Equals(ConvertibleClass? other)
   {
      if (ReferenceEquals(null, other))
         return false;
      if (ReferenceEquals(this, other))
         return true;
      return Key == other.Key;
   }

   public override bool Equals(object? obj)
   {
      if (ReferenceEquals(null, obj))
         return false;
      if (ReferenceEquals(this, obj))
         return true;
      if (obj.GetType() != this.GetType())
         return false;
      return Equals((ConvertibleClass)obj);
   }

   public override int GetHashCode()
   {
      return Key;
   }

   public override string ToString()
   {
      return Key.ToString();
   }
}
