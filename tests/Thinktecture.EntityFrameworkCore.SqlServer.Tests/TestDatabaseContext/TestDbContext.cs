using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;

// ReSharper disable InconsistentNaming
namespace Thinktecture.TestDatabaseContext;

public class TestDbContext : DbContext, IDbDefaultSchema
{
   /// <inheritdoc />
   public string? Schema { get; }

#nullable disable
   public DbSet<TestEntity> TestEntities { get; set; }
   public DbSet<TestTemporalTableEntity> TestTemporalTableEntity { get; set; }
   public DbSet<TestEntityWithAutoIncrement> TestEntitiesWithAutoIncrement { get; set; }
   public DbSet<TestEntityWithRowVersion> TestEntitiesWithRowVersion { get; set; }
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
   public IQueryable<TestViewEntity> TestView => Set<TestViewEntity>();
#nullable enable

   public Action<ModelBuilder>? ConfigureModel { get; set; }

   public TestDbContext(DbContextOptions<TestDbContext> options, IDbDefaultSchema? schema)
      : base(options)
   {
      Schema = schema?.Schema;
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      modelBuilder.ConfigureScalarCollectionParameter<ConvertibleClass>()
                  .Property(e => e.Value)
                  .HasConversion(c => c.Key, k => new ConvertibleClass(k));

      var myParamBuilder = modelBuilder.ConfigureComplexCollectionParameter<MyParameter>();
      myParamBuilder.Property(e => e.Column1)
                    .HasColumnName("Id");
      myParamBuilder.Property(e => e.Column2)
                    .HasConversion(c => c.Key, k => new ConvertibleClass(k));

      TestEntity.Configure(modelBuilder);

      modelBuilder.Entity<TestTemporalTableEntity>(builder => builder.ToTable("TestTemporalTableEntity", tableBuilder => tableBuilder.IsTemporal()));

      modelBuilder.Entity<TestViewEntity>(builder => builder.ToView("TestView"));

      modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).UseIdentityColumn();

      modelBuilder.Entity<TestEntityWithRowVersion>()
                  .Property(e => e.RowVersion)
                  .IsRowVersion()
                  .HasConversion(new NumberToBytesConverter<long>());

      TestEntityWithShadowProperties.Configure(modelBuilder);
      TestEntityWithSqlDefaultValues.Configure(modelBuilder);
      modelBuilder.Entity<TestEntityWithSqlDefaultValues>(builder => builder.Property(e => e.Id).HasDefaultValueSql("newid()"));

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

      ConfigureModel?.Invoke(modelBuilder);

      modelBuilder.Entity<InformationSchemaColumn>().HasNoKey().ToView("<<InformationSchemaColumn>>");
      modelBuilder.Entity<InformationSchemaTableConstraint>().HasNoKey().ToView("<<InformationSchemaTableConstraint>>");
      modelBuilder.Entity<InformationSchemaConstraintColumn>().HasNoKey().ToView("<<InformationSchemaConstraintColumn>>");
      modelBuilder.Entity<InformationSchemaKeyColumn>().HasNoKey().ToView("<<InformationSchemaKeyColumn>>");
   }

   public IQueryable<InformationSchemaColumn> GetTempTableColumns<T>()
      where T : class
   {
      var type = typeof(T);
      var entityType = Model.GetEntityType(type);
      return GetTempTableColumns(entityType);
   }

   public IQueryable<InformationSchemaColumn> GetTempTableColumns(IEntityType entityType)
   {
      var tableName = entityType.GetTableName() ?? throw new Exception($"The entity '{entityType.Name}' has no table name.");

      return GetTempTableColumns(tableName);
   }

   public IQueryable<InformationSchemaColumn> GetTempTableColumns(string tableName)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      if (!tableName.StartsWith("#", StringComparison.Ordinal))
         tableName = $"#{tableName}";

      return Set<InformationSchemaColumn>().FromSqlInterpolated($@"
SELECT
   *
FROM
   tempdb.INFORMATION_SCHEMA.COLUMNS WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
   }

   public IQueryable<InformationSchemaTableConstraint> GetTempTableConstraints<T>()
   {
      var tableName = this.GetEntityType<T>().GetTableName()
                      ?? throw new Exception("No table name");

      if (!tableName.StartsWith("#", StringComparison.Ordinal))
         tableName = $"#{tableName}";

      return Set<InformationSchemaTableConstraint>().FromSqlInterpolated($@"
SELECT
   *
FROM
   tempdb.INFORMATION_SCHEMA.TABLE_CONSTRAINTS WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
   }

   public IQueryable<InformationSchemaConstraintColumn> GetTempTableConstraintsColumns<T>()
   {
      var tableName = this.GetEntityType<T>().GetTableName() ?? throw new Exception("No table name.");

      if (!tableName.StartsWith("#", StringComparison.Ordinal))
         tableName = $"#{tableName}";

      return Set<InformationSchemaConstraintColumn>().FromSqlInterpolated($@"
SELECT
   *
FROM
   tempdb.INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
   }

   public IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns<T>()
   {
      var tableName = this.GetEntityType<T>().GetTableName() ?? throw new Exception("No table name.");

      return GetTempTableKeyColumns(tableName);
   }

   public IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns<TColumn1, TColumn2>()
   {
      var tableName = this.GetEntityType<TempTable<TColumn1, TColumn2>>().GetTableName() ?? throw new Exception("No table name.");

      return GetTempTableKeyColumns(tableName);
   }

   private IQueryable<InformationSchemaKeyColumn> GetTempTableKeyColumns(string tableName)
   {
      if (!tableName.StartsWith("#", StringComparison.Ordinal))
         tableName = $"#{tableName}";

      return Set<InformationSchemaKeyColumn>().FromSqlInterpolated($@"
SELECT
   *
FROM
   tempdb.INFORMATION_SCHEMA.KEY_COLUMN_USAGE WITH (NOLOCK)
WHERE
   OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID({"tempdb.." + tableName})");
   }
}
