namespace Thinktecture.TestDatabaseContext;

public class TestEntity
{
   public Guid Id { get; set; }
   public string? Name { get; set; }
   public int Count { get; set; }
   public ConvertibleClass? ConvertibleClass { get; set; }
   public BoundaryValueObject Boundary { get; set; } = new(1, 2);

   private int _propertyWithBackingField;

   public int PropertyWithBackingField
   {
      get => _propertyWithBackingField;
      set => _propertyWithBackingField = value;
   }

   private int _privateField;

   public int GetPrivateField()
   {
      return _privateField;
   }

   public void SetPrivateField(int value)
   {
      _privateField = value;
   }
}
