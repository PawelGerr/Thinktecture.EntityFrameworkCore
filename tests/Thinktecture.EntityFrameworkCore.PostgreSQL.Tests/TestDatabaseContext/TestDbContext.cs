using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore;

// ReSharper disable InconsistentNaming
namespace Thinktecture.TestDatabaseContext;

public class TestDbContext : DbContext, IDbDefaultSchema
{
   /// <inheritdoc />
   public string? Schema { get; }

#nullable disable
   // ReSharper disable UnusedAutoPropertyAccessor.Global
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
   public DbSet<TestEntityWithJsonColumns> TestEntitiesWithJsonColumns { get; set; }
   // ReSharper restore UnusedAutoPropertyAccessor.Global
#nullable enable

   public Action<ModelBuilder>? ConfigureModel { get; set; }
   public Action<DbContextOptionsBuilder>? Configure { get; set; }

   public TestDbContext(DbContextOptions<TestDbContext> options, IDbDefaultSchema? schema = null)
      : base(options)
   {
      Schema = schema?.Schema;
   }

   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
      base.OnConfiguring(optionsBuilder);

      Configure?.Invoke(optionsBuilder);
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      modelBuilder.ConfigureScalarCollectionParameter<ConvertibleClass>(builder => builder.Property(e => e.Value)
                                                                                          .HasConversion(c => c.Key, k => new ConvertibleClass(k)));

      modelBuilder.ConfigureComplexCollectionParameter<MyParameter>(myParamBuilder =>
                                                                    {
                                                                       myParamBuilder.Property(e => e.Column1)
                                                                                     .HasColumnName("Id");
                                                                       myParamBuilder.Property(e => e.Column2)
                                                                                     .HasConversion(c => c.Key, k => new ConvertibleClass(k));
                                                                    });

      TestEntity.Configure(modelBuilder);
      TestEntityWithBaseClass.Configure(modelBuilder);
      KeylessTestEntity.Configure(modelBuilder);

      modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).UseIdentityByDefaultColumn();

      TestEntityWithShadowProperties.Configure(modelBuilder);
      TestEntityWithSqlDefaultValues.Configure(modelBuilder);
      modelBuilder.Entity<TestEntityWithSqlDefaultValues>(builder => builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()"));

      TestEntityWithDotnetDefaultValues.Configure(modelBuilder);
      TestEntity_Owns_Inline.Configure(modelBuilder);
      TestEntity_Owns_SeparateOne.Configure(modelBuilder);
      TestEntity_Owns_SeparateMany.Configure(modelBuilder);
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
      TestEntityWithJsonColumns.Configure(modelBuilder);
      TestEntityWithDifferentColumnNames.Configure(modelBuilder);

      ConfigureModel?.Invoke(modelBuilder);

      modelBuilder.Entity<PgTempTableColumn>().HasNoKey().ToView("<<PgTempTableColumn>>");
      modelBuilder.Entity<PgTempTableConstraint>().HasNoKey().ToView("<<PgTempTableConstraint>>");
   }

   public IQueryable<PgTempTableColumn> GetTempTableColumns<T>()
      where T : class
   {
      var entityType = this.GetTempTableEntityType<T>();
      return GetTempTableColumns(entityType);
   }

   public IQueryable<PgTempTableColumn> GetTempTableColumns(IEntityType entityType)
   {
      var tableName = entityType.GetTableName() ?? throw new Exception($"The entity '{entityType.Name}' has no table name.");

      return GetTempTableColumns(tableName);
   }

   public IQueryable<PgTempTableColumn> GetTempTableColumns(string tableName)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      return Set<PgTempTableColumn>().FromSqlInterpolated($"""
                                                            SELECT
                                                               a.attname AS "ColumnName",
                                                               pg_catalog.format_type(a.atttypid, a.atttypmod) AS "DataType",
                                                               NOT a.attnotnull AS "IsNullable",
                                                               pg_catalog.pg_get_expr(d.adbin, d.adrelid) AS "ColumnDefault",
                                                               CAST(c.collname AS text) AS "CollationName"
                                                            FROM pg_catalog.pg_attribute a
                                                            JOIN pg_catalog.pg_class t ON t.oid = a.attrelid
                                                            JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace
                                                            LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
                                                            LEFT JOIN pg_catalog.pg_collation c ON c.oid = a.attcollation AND a.attcollation <> 0
                                                            WHERE t.relname = {tableName}
                                                              AND n.nspname LIKE 'pg_temp_%'
                                                              AND a.attnum > 0
                                                              AND NOT a.attisdropped
                                                            ORDER BY a.attnum
                                                            """);
   }

   public IQueryable<PgTempTableConstraint> GetTempTableConstraints<T>()
   {
      var tableName = this.GetTempTableEntityType<T>().GetTableName()
                      ?? throw new Exception("No table name");

      return GetTempTableConstraints(tableName);
   }

   public IQueryable<PgTempTableConstraint> GetTempTableConstraints(string tableName)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      return Set<PgTempTableConstraint>().FromSqlInterpolated($"""
                                                                SELECT
                                                                   con.conname AS "ConstraintName",
                                                                   CASE con.contype
                                                                      WHEN 'p' THEN 'PRIMARY KEY'
                                                                      WHEN 'u' THEN 'UNIQUE'
                                                                      WHEN 'c' THEN 'CHECK'
                                                                      WHEN 'f' THEN 'FOREIGN KEY'
                                                                      ELSE con.contype::text
                                                                   END AS "ConstraintType"
                                                                FROM pg_catalog.pg_constraint con
                                                                JOIN pg_catalog.pg_class t ON t.oid = con.conrelid
                                                                JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace
                                                                WHERE t.relname = {tableName}
                                                                  AND n.nspname LIKE 'pg_temp_%'
                                                                """);
   }
}
