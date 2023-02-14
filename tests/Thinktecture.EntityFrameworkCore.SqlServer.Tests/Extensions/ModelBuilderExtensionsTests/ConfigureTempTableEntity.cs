using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.ModelBuilderExtensionsTests;

// ReSharper disable once InconsistentNaming
public class ConfigureTempTableEntity : IntegrationTestsBase
{
   public ConfigureTempTableEntity(ITestOutputHelper testOutputHelper, SqlServerContainerFixture sqlServerContainerFixture)
      : base(testOutputHelper, sqlServerContainerFixture)
   {
   }

   [Fact]
   public void Should_introduce_temp_table_with_custom_type()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var entityType = ActDbContext.GetTempTableEntityType<CustomTempTable>();
      entityType.Should().NotBeNull();
      entityType.Name.Should().Be("Thinktecture:TempTable:Thinktecture.TestDatabaseContext.CustomTempTable");

      var properties = entityType.GetProperties().ToList();
      properties.Should().HaveCount(2);

      var intProperty = properties.First();
      intProperty.Name.Should().Be(nameof(CustomTempTable.Column1));
      intProperty.IsNullable.Should().BeFalse();
      intProperty.ClrType.Should().Be(typeof(int));

      var stringProperty = properties.Skip(1).First();
      stringProperty.Name.Should().Be(nameof(CustomTempTable.Column2));
      stringProperty.IsNullable.Should().BeTrue();
      stringProperty.ClrType.Should().Be(typeof(string));
   }

   [Fact]
   public void Should_generate_table_name_for_string()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var entityType = ActDbContext.GetTempTableEntityType<CustomTempTable>();
      entityType.Should().NotBeNull();
      entityType.GetTableName().Should().Be("#CustomTempTable");
   }
}
