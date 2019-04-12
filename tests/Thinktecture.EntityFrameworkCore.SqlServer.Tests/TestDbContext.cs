using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture
{
   public class TestDbContext : DbContext, IDbContextSchema
   {
      /// <inheritdoc />
      public string Schema { get; }

      public TestDbContext([NotNull] DbContextOptions<TestDbContext> options, [CanBeNull] IDbContextSchema schema)
         : base(options)
      {
         Schema = schema?.Schema;
      }
   }
}
