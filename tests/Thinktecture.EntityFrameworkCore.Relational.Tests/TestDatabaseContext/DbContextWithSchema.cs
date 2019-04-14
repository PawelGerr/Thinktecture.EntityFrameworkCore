using System;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext
{
   public class DbContextWithSchema : DbContext, IDbContextSchema
   {
      /// <inheritdoc />
      public string Schema { get; }

      public DbSet<TestEntity> TestEntities { get; set; }
      public DbQuery<TestQuery> TestQuery { get; set; }

      public Action<ModelBuilder> ConfigureModel { get; set; }

      public DbContextWithSchema(DbContextOptions<DbContextWithSchema> options, string schema)
         : base(options)
      {
         Schema = schema;
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         ConfigureModel?.Invoke(modelBuilder);
      }
   }
}
