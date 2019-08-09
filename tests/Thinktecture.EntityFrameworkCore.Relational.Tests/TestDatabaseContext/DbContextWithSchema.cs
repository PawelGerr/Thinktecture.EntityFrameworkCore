using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext
{
   public class DbContextWithSchema : DbContext, IDbDefaultSchema
   {
      /// <inheritdoc />
      public string Schema { get; }

      public DbSet<TestEntity> TestEntities { get; set; }
      public DbQuery<TestQuery> TestQuery { get; set; }

      public Action<ModelBuilder> ConfigureModel { get; set; }

      public DbContextWithSchema([NotNull] DbContextOptions<DbContextWithSchema> options, string schema)
         : base(options)
      {
         Schema = schema;
      }

      public static string TestDbFunction()
      {
         throw new NotSupportedException();
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.HasDbFunction(() => TestDbFunction());

         ConfigureModel?.Invoke(modelBuilder);
      }
   }
}
