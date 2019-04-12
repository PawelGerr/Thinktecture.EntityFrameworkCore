using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;
using Xunit;

namespace Thinktecture.ExtensionsTests.ModelBuilderExtensionsTests
{
   public class ConfigureTempTable_1_Column : TestBase
   {
      [Fact]
      public void Should_introduce_temp_table()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<int>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<int>));
         entityType.Should().NotBeNull();
         entityType.IsQueryType.Should().BeFalse();
      }

      [Fact]
      public void Should_introduce_temp_table_with_int()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<int>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<int>));
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<int>");

         var properties = entityType.GetProperties();
         properties.Should().HaveCount(1);

         var property = properties.First();
         property.Name.Should().Be("Column1");
         property.IsNullable.Should().BeFalse();
         property.IsKey().Should().BeTrue();
      }

      [Fact]
      public void Should_introduce_temp_table_with_nullable_int()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<int?>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<int?>));
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<System.Nullable<int>>");

         var properties = entityType.GetProperties();
         properties.Should().HaveCount(1);

         var property = properties.First();
         property.Name.Should().Be("Column1");
         property.IsNullable.Should().BeFalse(); // because it is the PK
         property.IsKey().Should().BeTrue();
      }

      [Fact]
      public void Should_introduce_temp_table_with_string()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<string>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<string>));
         entityType.Name.Should().Be("Thinktecture.EntityFrameworkCore.TempTables.TempTable<string>");

         var properties = entityType.GetProperties();
         properties.Should().HaveCount(1);

         var property = properties.First();
         property.Name.Should().Be("Column1");
         property.IsNullable.Should().BeFalse(); // because it is the PK
         property.IsKey().Should().BeTrue();
      }

      [Fact]
      public void Should_generate_table_name_for_int()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<int>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<int>));
         entityType.Relational().TableName.Should().Be("#TempTable<int>");
      }

      [Fact]
      public void Should_generate_table_name_for_nullable_int()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<int?>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<int?>));
         entityType.Relational().TableName.Should().Be("#TempTable<Nullable<int>>");
      }

      [Fact]
      public void Should_generate_table_name_for_string()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTable<string>();

         var entityType = DbContextWithSchema.Model.FindEntityType(typeof(TempTable<string>));
         entityType.Relational().TableName.Should().Be("#TempTable<string>");
      }
   }
}
