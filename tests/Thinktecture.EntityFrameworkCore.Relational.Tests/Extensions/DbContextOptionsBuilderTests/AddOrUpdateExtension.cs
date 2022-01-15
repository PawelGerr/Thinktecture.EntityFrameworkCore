using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextOptionsBuilderTests;

public class AddOrUpdateExtension
{
   [Fact]
   public void Should_throw_if_database_provider_not_registered()
   {
      new DbContextOptionsBuilder<DbContextWithoutSchema>()
         .Invoking(b => b.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(_ =>
                                                                                    {
                                                                                    }))
         .Should().Throw<InvalidOperationException>().WithMessage("Please register the database provider first (via 'UseSqlServer' or 'UseSqlite' etc).");
   }

   [Fact]
   public void Should_not_throw_if_database_provider_is_registered_already()
   {
      new DbContextOptionsBuilder<DbContextWithoutSchema>()
         .UseSqlite("DataSource=:memory:")
         .Invoking(b => b.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(_ =>
                                                                                    {
                                                                                    }))
         .Should().NotThrow();
   }

   [Fact]
   public void Should_not_throw_if_called_inside_database_provider_specific_callback()
   {
      new DbContextOptionsBuilder<DbContextWithoutSchema>()
         .UseSqlite("DataSource=:memory:",
                    sqliteBuilder => sqliteBuilder.Invoking(b => b.AddBulkOperationSupport()).Should().NotThrow());
   }
}
