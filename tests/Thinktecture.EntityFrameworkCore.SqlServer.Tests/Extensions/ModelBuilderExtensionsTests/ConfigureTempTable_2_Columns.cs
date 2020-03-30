using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.ModelBuilderExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   [Collection("BulkInsertTempTableAsync")]
   public class ConfigureTempTable_2_Columns : IntegrationTestsBase
   {
      public ConfigureTempTable_2_Columns(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public void Should_introduce_keyless_temp_table()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

         var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<int, int?>));
         entityType.Should().NotBeNull();
         entityType.GetKeys().Should().BeEmpty();
      }

      [Fact]
      public void Should_introduce_temp_table_with_int_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

         var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<int, int?>));
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<int, System.Nullable<int>>");

         var properties = entityType.GetProperties().ToList();
         properties.Should().HaveCount(2);

         properties.Select(p => p.IsNullable).Should().BeEquivalentTo(new[] { false, true });
         properties.First().Name.Should().Be("Column1");
         properties.Skip(1).First().Name.Should().Be("Column2");
      }

      [Fact]
      public void Should_introduce_temp_table_with_string_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string, string>();

         var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<string, string>));
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<string, string>");

         var properties = entityType.GetProperties().ToList();
         properties.Should().HaveCount(2);

         properties.Select(p => p.IsNullable).Should().BeEquivalentTo(new[] { false, false });
         properties.First().Name.Should().Be("Column1");
         properties.Skip(1).First().Name.Should().Be("Column2");
      }

      [Fact]
      public void Should_flag_col1_as_nullable_if_set_via_modelbuilder()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string, string>().Property(t => t.Column1).IsRequired(false);

         var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<string, string>));
         var properties = entityType.GetProperties();
         properties.Select(p => p.IsNullable).Should().BeEquivalentTo(new[] { true, false });
      }

      [Fact]
      public void Should_generate_table_name_for_int_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

         var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<int, int?>));
         entityType.GetTableName().Should().Be("#TempTable<int, Nullable<int>>");
      }

      [Fact]
      public void Should_generate_table_name_for_string_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string, string>();

         var entityType = ActDbContext.Model.FindEntityType(typeof(TempTable<string, string>));
         entityType.GetTableName().Should().Be("#TempTable<string, string>");
      }
   }
}
