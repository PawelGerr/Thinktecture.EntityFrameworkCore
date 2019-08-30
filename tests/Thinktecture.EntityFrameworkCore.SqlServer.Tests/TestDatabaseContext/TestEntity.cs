using System;

namespace Thinktecture.TestDatabaseContext
{
   public class TestEntity
   {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public int Count { get; set; }
      public ConvertibleClass ConvertibleClass { get; set; }

      private int _propertyWithBackingField;

      public int PropertyWithBackingField
      {
         get => _propertyWithBackingField;
         set => _propertyWithBackingField = value;
      }

#pragma warning disable 169, CA1823
      private int _privateField;
#pragma warning restore 169, CA1823

      public int GetPrivateField()
      {
         return _privateField;
      }

      public void SetPrivateField(int value)
      {
         _privateField = value;
      }
   }
}
