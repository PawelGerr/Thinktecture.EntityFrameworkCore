namespace Thinktecture.Database;

public class EntityWithByteArray
{
   public Guid Id { get; set; }
   public byte[] Bytes { get; set; }

   public EntityWithByteArray(Guid id, byte[] bytes)
   {
      Id = id;
      Bytes = bytes;
   }
}
