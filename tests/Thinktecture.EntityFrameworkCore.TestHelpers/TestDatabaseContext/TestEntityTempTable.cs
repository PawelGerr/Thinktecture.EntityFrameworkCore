using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Thinktecture.TestDatabaseContext;

/// <summary>
/// this type must have the same structure as <see cref="TestEntity"/>.
/// </summary>
public class TestEntityTempTable
{
   public Guid Id { get; set; }
   public string? Name { get; set; }
   public string RequiredName { get; set; } = String.Empty;
   public int Count { get; set; }
   public int? NullableCount { get; set; }
   public ConvertibleClass? ConvertibleClass { get; set; }

   public Guid? ParentId { get; set; }
   public TestEntityTempTable? Parent { get; set; }

   public List<TestEntityTempTable> Children { get; set; } = new();

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

   public static void Configure(EntityTypeBuilder<TestEntityTempTable> builder)
   {
      builder.HasKey(e => e.Id);

      builder.Property("_privateField");
      builder.Property(e => e.ConvertibleClass).HasConversion(c => c!.Key, k => new ConvertibleClass(k));
   }
}
