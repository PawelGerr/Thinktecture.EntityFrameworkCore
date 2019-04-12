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
      public void Should_introduce_temp_table_with_int_nullable_int()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<int, int?>));
         entityType.Should().NotBeNull();

         entityType.IsQueryType.Should().BeFalse();
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<int, System.Nullable<int>>");

         var properties = entityType.GetProperties();
         properties.Should().HaveCount(2);

         properties.Select(p => p.IsNullable).Should().BeEquivalentTo(new[] { false, false });
         properties.Select(p => p.IsKey()).Should().BeEquivalentTo(new[] { true, true });
         properties.First().Name.Should().Be("Column1");
         properties.Skip(1).First().Name.Should().Be("Column2");
      }

      [Fact]
      public void Should_introduce_temp_table_with_string_string()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<string, string>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<string, string>));
         entityType.Should().NotBeNull();

         entityType.IsQueryType.Should().BeFalse();
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<string, string>");

         var properties = entityType.GetProperties();
         properties.Should().HaveCount(2);

         properties.Select(p => p.IsNullable).Should().BeEquivalentTo(new[] { false, false });
         properties.Select(p => p.IsKey()).Should().BeEquivalentTo(new[] { true, true });
         properties.First().Name.Should().Be("Column1");
         properties.Skip(1).First().Name.Should().Be("Column2");
      }

      [Fact]
      public void Should_introduce_temp_table_with_string()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<string>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<string>));
         entityType.Should().NotBeNull();

         entityType.IsQueryType.Should().BeFalse();
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<string>");

         var properties = entityType.GetProperties();
         properties.Should().HaveCount(1);

         var property = properties.First();
         property.Name.Should().Be("Column1");
         property.IsNullable.Should().BeFalse(); // because it is the PK
         property.IsKey().Should().BeTrue();
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
