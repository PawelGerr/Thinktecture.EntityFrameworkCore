using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbContext : DbContext
   {
#nullable disable
      public DbSet<TestEntity> TestEntities { get; set; }
      public DbSet<TestEntityWithAutoIncrement> TestEntitiesWithAutoIncrement { get; set; }
      public DbSet<TestEntityWithShadowProperties> TestEntitiesWithShadowProperties { get; set; }
      public DbSet<TestEntityWithSqlDefaultValues> TestEntitiesWithDefaultValues { get; set; }
      public DbSet<TestEntityWithDotnetDefaultValues> TestEntitiesWithDotnetDefaultValues { get; set; }
      public DbSet<TestEntityOwningInlineEntity> TestEntitiesOwningInlineEntity { get; set; }
      public DbSet<TestEntityOwningOneSeparateEntity> TestEntitiesOwningOneSeparateEntity { get; set; }
      public DbSet<TestEntityOwningManyEntities> TestEntitiesOwningManyEntities { get; set; }
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

         modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).ValueGeneratedOnAdd();

         TestEntityWithShadowProperties.Configure(modelBuilder);
         TestEntityWithSqlDefaultValues.Configure(modelBuilder);
         TestEntityWithDotnetDefaultValues.Configure(modelBuilder);
         TestEntityOwningInlineEntity.Configure(modelBuilder);
         TestEntityOwningOneSeparateEntity.Configure(modelBuilder);
         TestEntityOwningManyEntities.Configure(modelBuilder);
         modelBuilder.Entity<TestEntityOwningManyEntities>().OwnsMany(e => e.SeparateEntities, b => b.Property("Id").ValueGeneratedNever());

         ConfigureModel?.Invoke(modelBuilder);

         modelBuilder.Entity<SqliteMaster>().HasNoKey().ToView("sqlite_temp_master");
         modelBuilder.Entity<SqliteTableInfo>().HasNoKey().ToView("PRAGMA_TABLE_INFO('<<table-name>>')");
         modelBuilder.Entity<SqliteIndex>().HasNoKey().ToView("pragma temp.index_list('<<table-name>>')");
      }

      public IQueryable<SqliteTableInfo> GetTempTableColumns<T>()
      {
         var entityType = Model.GetEntityType(typeof(T));
         return GetTempTableColumns(entityType);
      }

      public IQueryable<SqliteTableInfo> GetTempTableColumns(IEntityType entityType)
      {
         var tableName = entityType.GetTableName();

         return GetTempTableColumns(tableName);
      }

      public IQueryable<SqliteTableInfo> GetTempTableColumns(string tableName)
      {
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

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
