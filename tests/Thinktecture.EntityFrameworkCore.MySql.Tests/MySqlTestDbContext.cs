using System.Text.Json.Serialization;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture;

public sealed class MySqlTestDbContext(DbContextOptions<MySqlTestDbContext> options) : DbContext(options)
{
   public Action<ModelBuilder>? ConfigureModel { get; set; }
   public Action<DbContextOptionsBuilder>? Configure { get; set; }

   public DbSet<MySqlTestEntity> Entities => Set<MySqlTestEntity>();
   public DbSet<MySqlCompositeEntity> CompositeEntities => Set<MySqlCompositeEntity>();
   public DbSet<MySqlQuerySource> QuerySources => Set<MySqlQuerySource>();
   public DbSet<MySqlQueryTarget> QueryTargets => Set<MySqlQueryTarget>();
   public DbSet<MySqlTestChildEntity> Children => Set<MySqlTestChildEntity>();
   public DbSet<MySqlTypedEntity> TypedEntities => Set<MySqlTypedEntity>();
   public DbSet<MySqlOwnerEntity> Owners => Set<MySqlOwnerEntity>();
   public DbSet<MySqlCollatedEntity> CollatedEntities => Set<MySqlCollatedEntity>();
   public DbSet<MySqlAutoIncrementEntity> AutoIncrementEntities => Set<MySqlAutoIncrementEntity>();

   // Shared entities of Thinktecture.EntityFrameworkCore.TestHelpers (same names as in the other provider test projects).
   public DbSet<TestEntity> TestEntities => Set<TestEntity>();
   public DbSet<TestEntityWithBaseClass> TestEntitiesWithBaseClass => Set<TestEntityWithBaseClass>();
   public DbSet<KeylessTestEntity> KeylessEntities => Set<KeylessTestEntity>();
   public DbSet<TestEntityWithAutoIncrement> TestEntitiesWithAutoIncrement => Set<TestEntityWithAutoIncrement>();
   public DbSet<TestEntityWithShadowProperties> TestEntitiesWithShadowProperties => Set<TestEntityWithShadowProperties>();
   public DbSet<TestEntityWithSqlDefaultValues> TestEntitiesWithDefaultValues => Set<TestEntityWithSqlDefaultValues>();
   public DbSet<TestEntityWithDotnetDefaultValues> TestEntitiesWithDotnetDefaultValues => Set<TestEntityWithDotnetDefaultValues>();
   public DbSet<TestEntity_Owns_Inline> TestEntities_Own_Inline => Set<TestEntity_Owns_Inline>();
   public DbSet<TestEntity_Owns_Inline_Inline> TestEntities_Own_Inline_Inline => Set<TestEntity_Owns_Inline_Inline>();
   public DbSet<TestEntity_Owns_Inline_SeparateOne> TestEntities_Own_Inline_SeparateOne => Set<TestEntity_Owns_Inline_SeparateOne>();
   public DbSet<TestEntity_Owns_Inline_SeparateMany> TestEntities_Own_Inline_SeparateMany => Set<TestEntity_Owns_Inline_SeparateMany>();
   public DbSet<TestEntity_Owns_SeparateOne> TestEntities_Own_SeparateOne => Set<TestEntity_Owns_SeparateOne>();
   public DbSet<TestEntity_Owns_SeparateOne_Inline> TestEntities_Own_SeparateOne_Inline => Set<TestEntity_Owns_SeparateOne_Inline>();
   public DbSet<TestEntity_Owns_SeparateOne_SeparateOne> TestEntities_Own_SeparateOne_SeparateOne => Set<TestEntity_Owns_SeparateOne_SeparateOne>();
   public DbSet<TestEntity_Owns_SeparateOne_SeparateMany> TestEntities_Own_SeparateOne_SeparateMany => Set<TestEntity_Owns_SeparateOne_SeparateMany>();
   public DbSet<TestEntity_Owns_SeparateMany> TestEntities_Own_SeparateMany => Set<TestEntity_Owns_SeparateMany>();
   public DbSet<TestEntity_Owns_SeparateMany_Inline> TestEntities_Own_SeparateMany_Inline => Set<TestEntity_Owns_SeparateMany_Inline>();
   public DbSet<TestEntity_Owns_SeparateMany_SeparateOne> TestEntities_Own_SeparateMany_SeparateOne => Set<TestEntity_Owns_SeparateMany_SeparateOne>();
   public DbSet<TestEntity_Owns_SeparateMany_SeparateMany> TestEntities_Own_SeparateMany_SeparateMany => Set<TestEntity_Owns_SeparateMany_SeparateMany>();
   public DbSet<TestEntityWithComplexType> TestEntities_with_ComplexType => Set<TestEntityWithComplexType>();
   public DbSet<TestEntityWithJsonColumns> TestEntitiesWithJsonColumns => Set<TestEntityWithJsonColumns>();
   public DbSet<TestEntityWithFlags> TestEntitiesWithFlags => Set<TestEntityWithFlags>();
   public DbSet<TestEntityWithDifferentColumnNames> TestEntitiesWithDifferentColumnNames => Set<TestEntityWithDifferentColumnNames>();
   public DbSet<TestEntityWithCollation> TestEntitiesWithCollation => Set<TestEntityWithCollation>();

   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
      base.OnConfiguring(optionsBuilder);

      Configure?.Invoke(optionsBuilder);
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<MySqlAutoIncrementEntity>().Property(entity => entity.Name).HasMaxLength(100);
      modelBuilder.Entity<MySqlTypedEntity>(builder =>
                                            {
                                               builder.Property(entity => entity.Id).ValueGeneratedNever();
                                               builder.Property(entity => entity.Amount).HasPrecision(18, 4);
                                               builder.Property(entity => entity.Payload).HasColumnType("json");
                                               builder.Property(entity => entity.CreatedAt).HasColumnType("datetime(6)");
                                               builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(20);
                                            });
      modelBuilder.Entity<MySqlOwnerEntity>(builder =>
                                            {
                                               builder.Property(entity => entity.Id).ValueGeneratedNever();
                                               builder.OwnsOne(entity => entity.Address, owned => owned.ToTable("owner_addresses"));
                                               builder.OwnsMany(entity => entity.Phones, owned =>
                                                                                         {
                                                                                            owned.ToTable("owner_phones");
                                                                                            owned.Property<int>("PhoneId");
                                                                                            owned.HasKey("PhoneId");
                                                                                         });
                                            });
      modelBuilder.Entity<MySqlCollatedEntity>(builder =>
                                               {
                                                  builder.Property(entity => entity.Id).ValueGeneratedNever();
                                                  builder.Property(entity => entity.Name).HasMaxLength(100).HasCharSet("utf8mb4").UseCollation("utf8mb4_bin");
                                                  builder.HasIndex(entity => entity.Name);
                                               });
      modelBuilder.Entity<MySqlTestChildEntity>().HasOne<MySqlTestEntity>().WithMany().HasForeignKey(child => child.ParentId);
      modelBuilder.Entity<MySqlTestEntity>().Property(entity => entity.Name).HasMaxLength(255);
      modelBuilder.Entity<MySqlTestEntity>().HasIndex(entity => entity.Name).IsUnique();
      modelBuilder.Entity<MySqlCompositeEntity>().HasKey(entity => new { entity.RegionId, entity.ItemId });
      modelBuilder.Entity<MySqlCompositeEntity>().Property(entity => entity.Name).HasMaxLength(255);
      modelBuilder.Entity<MySqlQuerySource>().Property(entity => entity.Name).HasMaxLength(255);
      modelBuilder.Entity<MySqlQueryTarget>().Property(entity => entity.Name).HasMaxLength(255);
      modelBuilder.ConfigureComplexCollectionParameter<MySqlTestComplexParameter>();
      modelBuilder.ConfigureComplexCollectionParameter<MySqlTestJsonNamedComplexParameter>();
      modelBuilder.ConfigureTempTableEntity<MySqlTestTempEntity>(isKeyless: false, builder =>
                                                                                   {
                                                                                      builder.HasKey(entity => entity.Id);
                                                                                      builder.Property(entity => entity.Id).ValueGeneratedNever();
                                                                                      builder.Property(entity => entity.Name).HasMaxLength(255);
                                                                                   });
      ConfigureSharedEntities(modelBuilder);
      ConfigureModel?.Invoke(modelBuilder);
   }

   // The shared entities are written for the other providers; where they use provider-specific features, the MySQL equivalent is configured here.
   private static void ConfigureSharedEntities(ModelBuilder modelBuilder)
   {
      modelBuilder.ConfigureScalarCollectionParameter<ConvertibleClass>(builder => builder.Property(e => e.Value).HasConversion(c => c.Key, k => new ConvertibleClass(k)));
      modelBuilder.ConfigureComplexCollectionParameter<MyParameter>(builder =>
                                                                    {
                                                                       builder.Property(e => e.Column1).HasColumnName("Id");
                                                                       builder.Property(e => e.Column2).HasConversion(c => c.Key, k => new ConvertibleClass(k));
                                                                    });

      TestEntity.Configure(modelBuilder);
      TestEntityWithBaseClass.Configure(modelBuilder);
      KeylessTestEntity.Configure(modelBuilder);
      TestEntityWithShadowProperties.Configure(modelBuilder);
      TestEntityWithSqlDefaultValues.Configure(modelBuilder);
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
      TestEntityWithDifferentColumnNames.Configure(modelBuilder);
      TestEntityWithFlags.Configure(modelBuilder);

      // MySQL: UUID() is an expression default and must be enclosed in parentheses.
      modelBuilder.Entity<TestEntityWithSqlDefaultValues>(builder =>
                                                          {
                                                             builder.Property(e => e.Id).HasDefaultValueSql("(UUID())");
                                                             // TEXT columns can't have a literal default (error 1101), varchar can.
                                                             builder.Property(e => e.String).HasMaxLength(100);
                                                             builder.Property(e => e.NullableString).HasMaxLength(100);
                                                          });
      modelBuilder.Entity<TestEntityWithDotnetDefaultValues>(builder =>
                                                             {
                                                                builder.Property(e => e.String).HasMaxLength(100);
                                                                builder.Property(e => e.NullableString).HasMaxLength(100);
                                                             });

      // MySQL has no jsonb.
      modelBuilder.Entity<TestEntityWithJsonColumns>(builder =>
                                                     {
                                                        builder.Ignore(e => e.JsonbColumn);
                                                        builder.Property(e => e.JsonColumn).HasColumnType("json");
                                                     });

      // The shared entity configures a SQL Server collation.
      modelBuilder.Entity<TestEntityWithCollation>(builder =>
                                                   {
                                                      builder.Property(e => e.ColumnWithoutCollation).HasMaxLength(100);
                                                      builder.Property(e => e.ColumnWithCollation).HasMaxLength(100).HasCharSet("utf8mb4").UseCollation("utf8mb4_bin");
                                                   });
   }
}

public sealed class MySqlTestEntity
{
   public int Id { get; set; }
   public string? Name { get; set; }
}

public enum MySqlTestStatus
{
   Draft,
   Published
}

public sealed class MySqlAutoIncrementEntity
{
   public int Id { get; set; }
   public string? Name { get; set; }
}

public sealed class MySqlTypedEntity
{
   public Guid Id { get; set; }
   public decimal Amount { get; set; }
   public string? Payload { get; set; }
   public DateTime CreatedAt { get; set; }
   public MySqlTestStatus Status { get; set; }
   public byte[]? Data { get; set; }
   public bool Flag { get; set; }
}

public sealed class MySqlOwnerEntity
{
   public int Id { get; set; }
   public MySqlAddress? Address { get; set; }
   public List<MySqlPhone> Phones { get; set; } = [];
}

public sealed class MySqlAddress
{
   public string? City { get; set; }
}

public sealed class MySqlPhone
{
   public string? Number { get; set; }
}

public sealed class MySqlCollatedEntity
{
   public int Id { get; set; }
   public string? Name { get; set; }
   public int Count { get; set; }
}

public sealed class MySqlTestChildEntity
{
   public int Id { get; set; }
   public int ParentId { get; set; }
}

public sealed class MySqlCompositeEntity
{
   public int RegionId { get; set; }
   public int ItemId { get; set; }
   public string? Name { get; set; }
}

internal sealed class MySqlTestTempEntity
{
   public int Id { get; set; }
   public string? Name { get; set; }
}

public sealed class MySqlQuerySource
{
   public int Id { get; set; }
   public string? Name { get; set; }
   public int Count { get; set; }
}

public sealed class MySqlQueryTarget
{
   public int Id { get; set; }
   public string? Name { get; set; }
   public int Count { get; set; }
}

internal sealed record MySqlTestComplexParameter(int Id);

internal sealed record MySqlTestJsonNamedComplexParameter([property: JsonPropertyName("parameter_value")] int Value);
