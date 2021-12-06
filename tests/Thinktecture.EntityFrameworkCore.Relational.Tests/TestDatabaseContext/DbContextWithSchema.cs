using Thinktecture.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext;

public class DbContextWithSchema : DbContext, IDbDefaultSchema
{
   /// <inheritdoc />
   public string? Schema { get; }

#nullable disable
   public DbSet<TestEntity> TestEntities { get; set; }
   public DbSet<TestQuery> TestQuery { get; set; }
#nullable enable
   public Action<ModelBuilder>? ConfigureModel { get; set; }

   public DbContextWithSchema(DbContextOptions<DbContextWithSchema> options, string? schema)
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

      modelBuilder.Entity<TestQuery>().HasNoKey().ToView("TestQuery");

      modelBuilder.HasDbFunction(() => TestDbFunction());

      ConfigureModel?.Invoke(modelBuilder);
   }
}
