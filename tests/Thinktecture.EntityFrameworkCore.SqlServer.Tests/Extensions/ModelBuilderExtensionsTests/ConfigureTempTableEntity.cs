using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.ModelBuilderExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   public class ConfigureTempTableEntity : IntegrationTestsBase
   {
      public ConfigureTempTableEntity([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public void Should_introduce_temp_table_with_custom_type()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         var entityType = ActDbContext.Model.FindEntityType(typeof(CustomTempTable));
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
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         var entityType = ActDbContext.Model.FindEntityType(typeof(CustomTempTable));
         entityType.Relational().TableName.Should().Be("#CustomTempTable");
      }
   }
}
