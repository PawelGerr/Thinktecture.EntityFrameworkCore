using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.Extensions.ModelBuilderExtensionsTests;

// ReSharper disable once InconsistentNaming
public class ConfigureTempTable_1_Column : IntegrationTestsBase
{
   public ConfigureTempTable_1_Column(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, true)
   {
   }

   [Fact]
   public void Should_introduce_keyless_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<int>));
      entityType.Should().NotBeNull();
      entityType!.GetKeys().Should().BeEmpty();
   }

   [Fact]
   public void Should_introduce_temp_table_with_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<int>));
      entityType.Should().NotBeNull();
      entityType!.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<int>");

      var properties = entityType.GetProperties().ToList();
      properties.Should().HaveCount(1);

      var property = properties.First();
      property.Name.Should().Be("Column1");
      property.IsNullable.Should().BeFalse();
   }

   [Fact]
   public void Should_introduce_temp_table_with_nullable_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>();

      var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<int?>));
      entityType.Should().NotBeNull();
      entityType!.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<int?>");

      var properties = entityType.GetProperties();
      properties.Should().HaveCount(1);

      var property = properties.First();
      property.Name.Should().Be("Column1");
      property.IsNullable.Should().BeTrue();
   }

   [Fact]
   public void Should_flag_nullable_int_as_not_nullable_if_set_via_modelbuilder()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>().Property(t => t.Column1).IsRequired();

      var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<int?>));
      entityType.Should().NotBeNull();
      entityType!.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<int?>");

      var properties = entityType.GetProperties();
      properties.First().IsNullable.Should().BeFalse();
   }

   [Fact]
   public void Should_introduce_temp_table_with_string()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>().Property(t => t.Column1).IsRequired(false);

      var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<string>));
      entityType.Should().NotBeNull();
      entityType!.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<string>");

      var properties = entityType.GetProperties();
      properties.Should().HaveCount(1);

      var property = properties.First();
      property.Name.Should().Be("Column1");
      property.IsNullable.Should().BeTrue();
   }

   [Fact]
   public void Should_generate_table_name_for_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<int>));
      entityType.Should().NotBeNull();
      entityType!.GetTableName().Should().Be("#TempTable<int>");
   }

   [Fact]
   public void Should_generate_table_name_for_nullable_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>();

      var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<int?>));
      entityType.Should().NotBeNull();
      entityType!.GetTableName().Should().Be("#TempTable<int?>");
   }

   [Fact]
   public void Should_generate_table_name_for_string()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>();

      var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<string>));
      entityType.Should().NotBeNull();
      entityType!.GetTableName().Should().Be("#TempTable<string>");
   }
}
