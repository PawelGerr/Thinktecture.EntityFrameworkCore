using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbContext : DbContext
   {
#nullable disable
      public DbSet<TestEntity> TestEntities { get; set; }
      public DbSet<TestEntityWithAutoIncrement> TestEntitiesWithAutoIncrement { get; set; }
      public DbSet<TestEntityWithShadowProperties> TestEntitiesWithShadowProperties { get; set; }
#nullable enable

      public Action<ModelBuilder>? ConfigureModel { get; set; }

      public TestDbContext(DbContextOptions<TestDbContext> options)
         : base(options)
      {
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.Entity<TestEntity>().Property("_privateField");
         modelBuilder.Entity<TestEntity>().Property(e => e.ConvertibleClass).HasConversion(c => c!.Key, k => new ConvertibleClass(k));

         modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).ValueGeneratedOnAdd();

         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<string>("ShadowStringProperty").HasMaxLength(50);
         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<int?>("ShadowIntProperty");

         ConfigureModel?.Invoke(modelBuilder);

         modelBuilder.Entity<SqliteMaster>().HasNoKey().ToView("sqlite_temp_master");
         modelBuilder.Entity<SqliteTableInfo>().HasNoKey().ToView("PRAGMA_TABLE_INFO('<<table-name>>')");
         modelBuilder.Entity<SqliteIndex>().HasNoKey().ToView("pragma temp.index_list('<<table-name>>')");
      }

      public IQueryable<SqliteTableInfo> GetTempTableColumns<T>()
      {
         var tableName = Model.GetEntityType(typeof(T)).GetTableName();
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
}
