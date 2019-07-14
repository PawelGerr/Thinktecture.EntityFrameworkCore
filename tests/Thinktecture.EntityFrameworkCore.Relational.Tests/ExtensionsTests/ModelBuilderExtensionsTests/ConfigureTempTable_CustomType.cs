using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.ExtensionsTests.ModelBuilderExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   public class ConfigureTempTable_CustomType : TestBase
   {
      [Fact]
      public void Should_introduce_temp_table_with_custom_type()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureCustomTempTable<CustomTempTable>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(CustomTempTable));
         entityType.Name.Should().Be("Thinktecture.TestDatabaseContext.CustomTempTable");

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
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureCustomTempTable<CustomTempTable>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(CustomTempTable));
         entityType.Relational().TableName.Should().Be("#CustomTempTable");
      }
   }
}
