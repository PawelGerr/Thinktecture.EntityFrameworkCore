using System.Reflection;

namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class TestEntity
{
   public Guid Id { get; set; }
   public string? Name { get; set; }
   public string RequiredName { get; set; } = String.Empty;
   public int Count { get; set; }
   public int? NullableCount { get; set; }
   public ConvertibleClass? ConvertibleClass { get; set; }

   public Guid? ParentId { get; set; }
   public TestEntity? Parent { get; set; }

   public List<TestEntity> Children { get; set; } = new();

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

   public static IReadOnlyList<MemberInfo> GetRequiredProperties()
   {
      return new MemberInfo[]
             {
                typeof(TestEntity).GetProperty(nameof(Id)) ?? throw new Exception($"Property {nameof(Id)} not found."),
                typeof(TestEntity).GetProperty(nameof(RequiredName)) ?? throw new Exception($"Property {nameof(RequiredName)} not found."),
                typeof(TestEntity).GetProperty(nameof(Count)) ?? throw new Exception($"Property {nameof(Count)} not found."),
                typeof(TestEntity).GetProperty(nameof(PropertyWithBackingField)) ?? throw new Exception($"Property {nameof(PropertyWithBackingField)} not found."),
                typeof(TestEntity).GetField("_privateField", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new Exception("Field _privateField not found.")
             };
   }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity>(builder =>
                                      {
                                         builder.Property("_privateField");
                                         builder.Property(e => e.ConvertibleClass).HasConversion(c => c!.Key, k => new ConvertibleClass(k));

                                         builder.HasIndex(e => e.Id)
                                                .HasDatabaseName("IX_TestEntities_Id");
                                      });
   }
}
