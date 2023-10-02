using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable InconsistentNaming
namespace Thinktecture.TestDatabaseContext;

public class TestDbContext : DbContext
{
#nullable disable
   public DbSet<TestEntity> TestEntities { get; set; }
   public DbSet<TestEntityWithBaseClass> TestEntitiesWithBaseClass { get; set; }
   public DbSet<KeylessTestEntity> KeylessEntities { get; set; }
   public DbSet<TestEntityWithAutoIncrement> TestEntitiesWithAutoIncrement { get; set; }
   public DbSet<TestEntityWithShadowProperties> TestEntitiesWithShadowProperties { get; set; }
   public DbSet<TestEntityWithSqlDefaultValues> TestEntitiesWithDefaultValues { get; set; }
   public DbSet<TestEntityWithDotnetDefaultValues> TestEntitiesWithDotnetDefaultValues { get; set; }
   public DbSet<TestEntity_Owns_Inline> TestEntities_Own_Inline { get; set; }
   public DbSet<TestEntity_Owns_Inline_Inline> TestEntities_Own_Inline_Inline { get; set; }
   public DbSet<TestEntity_Owns_Inline_SeparateOne> TestEntities_Own_Inline_SeparateOne { get; set; }
   public DbSet<TestEntity_Owns_Inline_SeparateMany> TestEntities_Own_Inline_SeparateMany { get; set; }
   public DbSet<TestEntity_Owns_SeparateOne> TestEntities_Own_SeparateOne { get; set; }
   public DbSet<TestEntity_Owns_SeparateOne_Inline> TestEntities_Own_SeparateOne_Inline { get; set; }
   public DbSet<TestEntity_Owns_SeparateOne_SeparateOne> TestEntities_Own_SeparateOne_SeparateOne { get; set; }
   public DbSet<TestEntity_Owns_SeparateOne_SeparateMany> TestEntities_Own_SeparateOne_SeparateMany { get; set; }
   public DbSet<TestEntity_Owns_SeparateMany> TestEntities_Own_SeparateMany { get; set; }
   public DbSet<TestEntity_Owns_SeparateMany_Inline> TestEntities_Own_SeparateMany_Inline { get; set; }
   public DbSet<TestEntity_Owns_SeparateMany_SeparateOne> TestEntities_Own_SeparateMany_SeparateOne { get; set; }
   public DbSet<TestEntity_Owns_SeparateMany_SeparateMany> TestEntities_Own_SeparateMany_SeparateMany { get; set; }
   public DbSet<TestEntityWithComplexType> TestEntities_with_ComplexType { get; set; }
#nullable enable

   public Action<ModelBuilder>? ConfigureModel { get; set; }

   public TestDbContext(DbContextOptions<TestDbContext> options)
      : base(options)
   {
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      TestEntity.Configure(modelBuilder);
      TestEntityWithBaseClass.Configure(modelBuilder);
      KeylessTestEntity.Configure(modelBuilder);

      modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).ValueGeneratedOnAdd();

      TestEntityWithShadowProperties.Configure(modelBuilder);
      TestEntityWithSqlDefaultValues.Configure(modelBuilder);
      TestEntityWithDotnetDefaultValues.Configure(modelBuilder);
      TestEntity_Owns_Inline.Configure(modelBuilder);
      TestEntity_Owns_SeparateOne.Configure(modelBuilder);
      TestEntity_Owns_SeparateMany.Configure(modelBuilder);
      modelBuilder.Entity<TestEntity_Owns_SeparateMany>().OwnsMany(e => e.SeparateEntities, b => b.Property("Id").ValueGeneratedNever());
      TestEntity_Owns_SeparateOne_SeparateOne.Configure(modelBuilder);
      TestEntity_Owns_SeparateOne_Inline.Configure(modelBuilder);
      TestEntity_Owns_SeparateOne_SeparateMany.Configure(modelBuilder);
      TestEntity_Owns_Inline_SeparateOne.Configure(modelBuilder);
      TestEntity_Owns_Inline_Inline.Configure(modelBuilder);
      TestEntity_Owns_Inline_SeparateMany.Configure(modelBuilder);
      TestEntity_Owns_SeparateMany_SeparateOne.Configure(modelBuilder);
      TestEntity_Owns_SeparateMany_Inline.Configure(modelBuilder);
      TestEntity_Owns_SeparateMany_SeparateMany.Configure(modelBuilder);
      TestEntityWithComplexType.Configure(modelBuilder);

      ConfigureModel?.Invoke(modelBuilder);

      modelBuilder.Entity<SqliteMaster>().HasNoKey().ToView("sqlite_temp_master");
      modelBuilder.Entity<SqliteTableInfo>().HasNoKey().ToView("PRAGMA_TABLE_INFO('<<table-name>>')");
      modelBuilder.Entity<SqliteIndex>().HasNoKey().ToView("pragma temp.index_list('<<table-name>>')");
   }

   public IQueryable<SqliteTableInfo> GetTempTableColumns<T>()
   {
      var entityType = this.GetTempTableEntityType<T>();
      return GetTempTableColumns(entityType);
   }

   public IQueryable<SqliteTableInfo> GetTempTableColumns(IEntityType entityType)
   {
      var tableName = entityType.GetTableName() ?? throw new Exception($"The entity '{entityType.Name}' has no table name.");

      return GetTempTableColumns(tableName);
   }

   public IQueryable<SqliteTableInfo> GetTempTableColumns(string tableName)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      var helper = this.GetService<ISqlGenerationHelper>();

      return Set<SqliteTableInfo>()
         .FromSqlRaw($"SELECT * FROM PRAGMA_TABLE_INFO({helper.DelimitIdentifier(tableName)})");
   }

   public IQueryable<SqliteTableInfo> GetTempTableKeyColumns<T>()
   {
      return GetTempTableColumns<T>()
         .Where(c => c.PK > 0);
   }
}
