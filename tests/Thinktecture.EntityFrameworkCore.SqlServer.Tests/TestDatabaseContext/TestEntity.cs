using System;
using System.Collections.Generic;

namespace Thinktecture.TestDatabaseContext
{
   public class TestEntity
   {
      public Guid Id { get; set; }
      public string? Name { get; set; }
      public int Count { get; set; }
      public ConvertibleClass? ConvertibleClass { get; set; }

      public Guid? ParentId { get; set; }
      public TestEntity? Parent { get; set; }

      public List<TestEntity> Children { get; set; }

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
}
