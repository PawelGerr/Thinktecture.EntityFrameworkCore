using System;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.ExtensionsTests.ModelBuilderExtensionsTests
{
   public class SetSchema : TestBase
   {
      public SetSchema([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Should_throw_if_builder_is_null()
      {
         ((ModelBuilder)null).Invoking(b => b.SetSchema("schema")).Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_set_schema()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.SetSchema("schema");

         DbContextWithSchema.Model.FindEntityType(typeof(TestEntity)).Relational().Schema.Should().Be("schema");
         DbContextWithSchema.Model.FindEntityType(typeof(TestQuery)).Relational().Schema.Should().Be("schema");
      }

      [Fact]
      public void Should_reset_schema()
      {
         DbContextWithSchema.ConfigureModel = builder =>
                                                    {
                                                       builder.Entity<TestEntity>().ToTable("table", "schema");
                                                       builder.Query<TestQuery>().ToView("view", "schema");
                                                       builder.SetSchema(null);
                                                    };

         DbContextWithSchema.Model.FindEntityType(typeof(TestEntity)).Relational().Schema.Should().BeNull();
         DbContextWithSchema.Model.FindEntityType(typeof(TestQuery)).Relational().Schema.Should().BeNull();
      }
   }
}
