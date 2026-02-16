namespace Thinktecture.Extensions.NpgsqlOperationBuilderExtensionsTests;

public class MigrationExtensionsTestDbContext : DbContext
{
   public MigrationExtensionsTestDbContext(DbContextOptions<MigrationExtensionsTestDbContext> options)
      : base(options)
   {
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<PgInformationSchemaColumn>().HasNoKey();
      modelBuilder.Entity<PgIdentityColumn>().HasNoKey();
      modelBuilder.Entity<PgIndex>().HasNoKey();
      modelBuilder.Entity<PgUniqueConstraint>().HasNoKey();
   }

   public IQueryable<PgInformationSchemaColumn> GetTableColumns(string tableName, string schema = "public")
   {
      ArgumentNullException.ThrowIfNull(tableName);

      return Set<PgInformationSchemaColumn>().FromSqlInterpolated($"""
                                                                   SELECT
                                                                      column_name AS "Column_Name"
                                                                   FROM
                                                                      information_schema.columns
                                                                   WHERE
                                                                      table_schema = {schema}
                                                                      AND table_name = {tableName}
                                                                   """);
   }

   public IQueryable<PgIndex> GetIndexes(string tableName, string schema = "public")
   {
      ArgumentNullException.ThrowIfNull(tableName);

      return Set<PgIndex>().FromSqlInterpolated($"""
                                                 SELECT
                                                    indexname AS "Indexname",
                                                    indexdef AS "Indexdef"
                                                 FROM
                                                    pg_indexes
                                                 WHERE
                                                    schemaname = {schema}
                                                    AND tablename = {tableName}
                                                 """);
   }

   public IQueryable<PgUniqueConstraint> GetUniqueConstraints(string constraintName, string schema = "public")
   {
      ArgumentNullException.ThrowIfNull(constraintName);

      return Set<PgUniqueConstraint>().FromSqlInterpolated($"""
                                                            SELECT
                                                               constraint_name AS "Constraint_Name"
                                                            FROM
                                                               information_schema.table_constraints
                                                            WHERE
                                                               constraint_schema = {schema}
                                                               AND constraint_name = {constraintName}
                                                               AND constraint_type = 'UNIQUE'
                                                            """);
   }

   public IQueryable<PgIdentityColumn> GetIdentityColumns(string tableName, string schema = "public")
   {
      ArgumentNullException.ThrowIfNull(tableName);

      return Set<PgIdentityColumn>().FromSqlInterpolated($"""
                                                           SELECT
                                                              column_name AS "Column_Name",
                                                              is_identity AS "Is_Identity"
                                                           FROM
                                                              information_schema.columns
                                                           WHERE
                                                              table_schema = {schema}
                                                              AND table_name = {tableName}
                                                              AND is_identity = 'YES'
                                                           """);
   }
}
