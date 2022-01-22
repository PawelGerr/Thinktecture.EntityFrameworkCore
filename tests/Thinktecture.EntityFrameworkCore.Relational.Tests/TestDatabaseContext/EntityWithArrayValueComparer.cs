namespace Thinktecture.TestDatabaseContext;

public class EntityWithArrayValueComparer
{
   public Guid Id { get; set; }
   public byte[] Bytes { get; set; }

   public EntityWithArrayValueComparer(Guid id, byte[] bytes)
   {
      Id = id;
      Bytes = bytes;
   }
}
