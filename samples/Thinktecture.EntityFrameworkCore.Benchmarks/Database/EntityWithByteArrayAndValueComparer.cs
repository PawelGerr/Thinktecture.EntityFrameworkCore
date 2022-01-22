namespace Thinktecture.Database;

public class EntityWithByteArrayAndValueComparer
{
   public Guid Id { get; set; }
   public byte[] Bytes { get; set; }

   public EntityWithByteArrayAndValueComparer(Guid id, byte[] bytes)
   {
      Id = id;
      Bytes = bytes;
   }
}
