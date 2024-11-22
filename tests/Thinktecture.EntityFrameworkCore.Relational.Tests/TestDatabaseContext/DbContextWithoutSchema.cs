namespace Thinktecture.TestDatabaseContext;

public class DbContextWithoutSchema : DbContext
{
   public Action<ModelBuilder>? ConfigureModel { get; set; }

   public DbContextWithoutSchema(DbContextOptions<DbContextWithoutSchema> options)
      : base(options)
   {
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      ConfigureModel?.Invoke(modelBuilder);
   }
}
