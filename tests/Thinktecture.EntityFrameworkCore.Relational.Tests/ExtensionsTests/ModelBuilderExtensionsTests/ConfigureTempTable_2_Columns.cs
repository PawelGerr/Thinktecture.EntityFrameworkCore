using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;
using Xunit;

namespace Thinktecture.ExtensionsTests.ModelBuilderExtensionsTests
{
   public class ConfigureTempTable_2_Columns : TestBase
   {
      [Fact]
      public void Should_introduce_query_type()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<int, int?>));
         entityType.Should().NotBeNull();
         entityType.IsQueryType.Should().BeTrue();
      }

      [Fact]
      public void Should_introduce_temp_table_with_int_nullable_int()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<int, int?>));
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<int, System.Nullable<int>>");

         var properties = entityType.GetProperties();
         properties.Should().HaveCount(2);

         properties.Select(p => p.IsNullable).Should().BeEquivalentTo(new[] { false, true });
         properties.First().Name.Should().Be("Column1");
         properties.Skip(1).First().Name.Should().Be("Column2");
      }

      [Fact]
      public void Should_introduce_temp_table_with_string_string()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<string, string>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<string, string>));
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<string, string>");

         var properties = entityType.GetProperties();
         properties.Should().HaveCount(2);

         properties.Select(p => p.IsNullable).Should().BeEquivalentTo(new[] { true, true });
         properties.First().Name.Should().Be("Column1");
         properties.Skip(1).First().Name.Should().Be("Column2");
      }

      [Fact]
      public void Should_flag_col2_as_not_nullable_if_set_via_modelbuilder()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<string, string>().Property(t => t.Column2).IsRequired();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<string, string>));
         var properties = entityType.GetProperties();
         properties.Select(p => p.IsNullable).Should().BeEquivalentTo(new[] { true, false });
      }

      [Fact]
      public void Should_generate_table_name_for_int_nullable_int()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<int, int?>));
         entityType.Relational().TableName.Should().Be("#TempTable<int, Nullable<int>>");
      }

      [Fact]
      public void Should_generate_table_name_for_string_string()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<string, string>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<string, string>));
         entityType.Relational().TableName.Should().Be("#TempTable<string, string>");
      }
   }
}
