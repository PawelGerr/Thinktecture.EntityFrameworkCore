using System;
using System.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;

namespace Thinktecture.EntityFrameworkCore
{
   public abstract class SqlServerDbContextIntegrationTests<T> : IDisposable
      where T : DbContext
   {
      private const string _HISTORY_TABLE_NAME = "__EFMigrationsHistory";

      private readonly string _connectionString;
      private readonly bool _useSharedTables;

      private string _schema;
      private T _dbContext;
      private IDbContextTransaction _tx;

      [NotNull]
      protected string Schema => _schema ?? (_schema = GetSchema(_useSharedTables));

      [NotNull]
      protected T DbContext => _dbContext ?? (_dbContext = CreateContext());

      protected SqlServerDbContextIntegrationTests([NotNull] string connectionString, bool useSharedTables)
      {
         _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
         _useSharedTables = useSharedTables;
      }

      [NotNull]
      protected abstract T CreateContext([NotNull] DbContextOptions<T> options, [NotNull] IDbContextSchema schema);

      [NotNull]
      protected virtual string GetSchema(bool useSharedTables)
      {
         return useSharedTables ? "tests" : Guid.NewGuid().ToString("N");
      }

      [NotNull]
      private T CreateContext()
      {
         var optionsBuilder = CreateOptionsBuilder(_connectionString);
         var ctx = CreateContext(optionsBuilder.Options, new DbContextSchema(Schema));
         ctx.Database.Migrate();

         if (_useSharedTables)
            _tx = ctx.Database.BeginTransaction(IsolationLevel.ReadCommitted);

         return ctx;
      }

      [NotNull]
      protected virtual DbContextOptionsBuilder<T> CreateOptionsBuilder([NotNull] string connString)
      {
         if (connString == null)
            throw new ArgumentNullException(nameof(connString));

         return new DbContextOptionsBuilder<T>()
                .UseSqlServer(connString, ConfigureSqlServer)
                .ReplaceService<IMigrationsSqlGenerator, ThinktectureSqlServerMigrationsSqlGenerator>()
                .ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>()
                .ReplaceService<IModelCacheKeyFactory, DbSchemaAwareModelCacheKeyFactory>();
      }

      protected virtual void ConfigureSqlServer([NotNull] SqlServerDbContextOptionsBuilder builder)
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         builder.MigrationsHistoryTable(_HISTORY_TABLE_NAME, Schema);
      }

      public virtual void Dispose()
      {
         if (_dbContext == null)
            return;

         if (_useSharedTables)
         {
            _tx?.Rollback();
            _tx?.Dispose();
         }
         else
         {
            RollbackMigrations(_dbContext);
            CleanUpDatabase(_dbContext, Schema);
         }

         _dbContext.Dispose();
      }

      private static void RollbackMigrations([NotNull] T ctx)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         ctx.GetService<IMigrator>().Migrate("0");
      }

      private static void CleanUpDatabase([NotNull] T ctx, [NotNull] string schema)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (schema == null)
            throw new ArgumentNullException(nameof(schema));

         ctx.Database.ExecuteSqlCommand((string)$@"
DECLARE @crlf NVARCHAR(MAX) = CHAR(13) + CHAR(10);
DECLARE @schema NVARCHAR(MAX) = '{schema}';
DECLARE @sql NVARCHAR(MAX);
DECLARE @cursor CURSOR

-- Drop Constraints
SET @cursor = CURSOR FAST_FORWARD FOR
SELECT DISTINCT sql = 'ALTER TABLE ' + QUOTENAME(tc.TABLE_SCHEMA) + '.' +  QUOTENAME(tc.TABLE_NAME) + ' DROP ' + QUOTENAME(rc.CONSTRAINT_NAME) + ';' + @crlf
FROM
	INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
	LEFT JOIN
		INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
		ON tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
WHERE
   tc.TABLE_SCHEMA = @schema

OPEN @cursor FETCH NEXT FROM @cursor INTO @sql

WHILE (@@FETCH_STATUS = 0)
BEGIN
Exec sp_executesql @sql
FETCH NEXT FROM @cursor INTO @sql
END

CLOSE @cursor 
DEALLOCATE @cursor

-- Drop Views
SELECT	@sql = N'';
SELECT @sql = @sql + 'DROP VIEW ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) +';' + @crlf
FROM
	SYS.VIEWS
WHERE
   schema_id = SCHEMA_ID(@schema)

EXEC(@sql);

-- Drop Functions
SELECT @sql = N'';
SELECT @sql = @sql + N' DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name)
FROM sys.objects
WHERE type_desc LIKE '%FUNCTION%'
   AND schema_id = SCHEMA_ID(@schema);

EXEC(@sql);

-- Drop tables
SELECT	@sql = N'';
SELECT @sql = @sql + 'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) +';' + @crlf
FROM
	SYS.TABLES
WHERE
   schema_id = SCHEMA_ID(@schema)

EXEC(@sql);
");
         ctx.Database.ExecuteSqlCommand((string)$"DROP SCHEMA [{schema}]");
      }
   }
}
