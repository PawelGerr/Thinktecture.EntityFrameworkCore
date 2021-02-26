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

         modelBuilder.Entity<TestEntity>(builder =>
                                         {
                                            builder.Property("_privateField");
                                            builder.Property(e => e.ConvertibleClass).HasConversion(c => c!.Key, k => new ConvertibleClass(k));
                                         });

         modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).ValueGeneratedOnAdd();

         modelBuilder.Entity<TestEntityWithShadowProperties>(builder =>
                                                             {
                                                                builder.Property<string>("ShadowStringProperty").HasMaxLength(50);
                                                                builder.Property<int?>("ShadowIntProperty");
                                                             });

         modelBuilder.Entity<TestEntityWithSqlDefaultValues>(builder =>
                                                             {
                                                                builder.Property(e => e.Int).HasDefaultValueSql("1");
                                                                builder.Property(e => e.NullableInt).HasDefaultValueSql("2");
                                                                builder.Property(e => e.String).HasDefaultValueSql("'3'");
                                                                builder.Property(e => e.NullableString).HasDefaultValueSql("'4'");
                                                             });

         modelBuilder.Entity<TestEntityWithDotnetDefaultValues>(builder =>
                                                                {
                                                                   builder.Property(e => e.Id).HasDefaultValue(new Guid("0B151271-79BB-4F6C-B85F-E8F61300FF1B"));
                                                                   builder.Property(e => e.Int).HasDefaultValue(1);
                                                                   builder.Property(e => e.NullableInt).HasDefaultValue(2);
                                                                   builder.Property(e => e.String).HasDefaultValue("3");
                                                                   builder.Property(e => e.NullableString).HasDefaultValue("4");
                                                                });

         modelBuilder.Entity<TestEntityOwningInlineEntity>(builder => builder.OwnsOne(e => e.InlineEntity));
         modelBuilder.Entity<TestEntityOwningOneSeparateEntity>(builder => builder.OwnsOne(e => e.SeparateEntity,
                                                                                           navigationBuilder => navigationBuilder.ToTable("SeparateEntities_One")));
         modelBuilder.Entity<TestEntityOwningManyEntities>(builder => builder.OwnsMany(e => e.SeparateEntities,
                                                                                       navigationBuilder => navigationBuilder.ToTable("SeparateEntities_Many")));

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
