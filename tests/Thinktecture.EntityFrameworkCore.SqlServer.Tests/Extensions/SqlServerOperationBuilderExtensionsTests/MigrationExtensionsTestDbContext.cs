using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.SqlServerOperationBuilderExtensionsTests;

public class MigrationExtensionsTestDbContext : DbContext
{
   public MigrationExtensionsTestDbContext(DbContextOptions<MigrationExtensionsTestDbContext> options)
      : base(options)
   {
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<InformationSchemaColumn>().HasNoKey();
      modelBuilder.Entity<SysIndex>().HasNoKey();
      modelBuilder.Entity<SysIndexColumn>().HasNoKey();
      modelBuilder.Entity<UniqueConstraint>().HasNoKey();
   }

   public IQueryable<InformationSchemaColumn> GetTableColumns(string tableName)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      return Set<InformationSchemaColumn>().FromSqlInterpolated($"""
                                                                 SELECT
                                                                    *
                                                                 FROM
                                                                    INFORMATION_SCHEMA.COLUMNS WITH (NOLOCK)
                                                                 WHERE
                                                                    OBJECT_ID(TABLE_NAME) = OBJECT_ID({tableName})
                                                                 """);
   }

   public IQueryable<SysIndex> GetIndexes(string tableName)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      return Set<SysIndex>().FromSqlInterpolated($"""
                                                  SELECT
                                                     *
                                                  FROM
                                                     SYS.INDEXES WITH (NOLOCK)
                                                  WHERE
                                                     OBJECT_ID = OBJECT_ID({tableName})
                                                     AND Index_Id <> 0
                                                  """);
   }

   public IQueryable<SysIndexColumn> GetIndexColumns(string tableName, int indexId)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      return Set<SysIndexColumn>().FromSqlInterpolated($"""
                                                        SELECT
                                                           c.*
                                                        FROM
                                                           SYS.INDEXES AS i
                                                           INNER JOIN SYS.INDEX_COLUMNS AS c
                                                              ON i.object_id = c.object_id
                                                              AND i.index_id = c.index_id
                                                        WHERE
                                                           i.object_id = OBJECT_ID({tableName})
                                                           AND i.index_id = {indexId};
                                                        """);
   }

   public IQueryable<UniqueConstraint> GetUniqueConstraints(string constraintName)
   {
      ArgumentNullException.ThrowIfNull(constraintName);

      return Set<UniqueConstraint>().FromSqlInterpolated($"""
                                                          SELECT
                                                             *
                                                          FROM
                                                             INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                                                          WHERE
                                                             CONSTRAINT_TYPE = 'UNIQUE'
                                                             AND CONSTRAINT_NAME = {constraintName};
                                                          """);
   }
}
