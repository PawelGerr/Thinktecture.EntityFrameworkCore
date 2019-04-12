using System;
using System.Collections.Concurrent;
using System.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;

namespace Thinktecture.EntityFrameworkCore
{
   /// <summary>
   /// A base class for integration tests using EF Core along with SQL Server.
   /// </summary>
   /// <typeparam name="T">Type of the database context.</typeparam>
   // ReSharper disable once UnusedMember.Global
   public abstract class SqlServerDbContextIntegrationTests<T> : IDisposable
      where T : DbContext
   {
      private const string _HISTORY_TABLE_NAME = "__EFMigrationsHistory";

      // ReSharper disable once StaticMemberInGenericType because the locks are all for the same database context but for different schemas.
      private static readonly ConcurrentDictionary<string, object> _locks = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

      private readonly string _connectionString;
      private readonly bool _useSharedTables;

      private string _schema;
      private T _dbContext;
      private IDbContextTransaction _tx;
      private ILoggerFactory _loggerFactory;

      /// <summary>
      /// Database schema in use.
      /// </summary>
      [NotNull]
      // ReSharper disable once MemberCanBePrivate.Global
      protected string Schema => _schema ?? (_schema = GetSchema(_useSharedTables));

      /// <summary>
      /// Database context being used for the tests.
      /// </summary>
      [NotNull]
      // ReSharper disable once UnusedMember.Global
      protected T DbContext => _dbContext ?? (_dbContext = CreateContext());

      /// <summary>
      /// Initializes a new instance of <see cref="SqlServerDbContextIntegrationTests{T}"/>
      /// </summary>
      /// <param name="connectionString">Connection string to use.</param>
      /// <param name="useSharedTables">Indication whether new tables with a new schema should be created or not.</param>
      protected SqlServerDbContextIntegrationTests([NotNull] string connectionString, bool useSharedTables)
      {
         _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
         _useSharedTables = useSharedTables;
      }

      /// <summary>
      /// Creates a new instance of the database context.
      /// </summary>
      /// <param name="options">Options to use for creation.</param>
      /// <param name="schema">Database schema to use.</param>
      /// <returns>A new instance of the database context.</returns>
      [NotNull]
      protected abstract T CreateContext([NotNull] DbContextOptions<T> options, [NotNull] IDbContextSchema schema);

      /// <summary>
      /// Gets/generates schema to be used.
      /// </summary>
      /// <param name="useSharedTables">Indication whether a new schema should be generated or a shared one.</param>
      /// <returns>A database schema.</returns>
      [NotNull]
      protected virtual string GetSchema(bool useSharedTables)
      {
         return useSharedTables ? "tests" : Guid.NewGuid().ToString("N");
      }

      /// <summary>
      /// Sets or resets the logger factory to be used by EF.
      /// </summary>
      /// <param name="loggerFactory">Logger factory to use.</param>
      // ReSharper disable once UnusedMember.Global
      protected void UseLoggerFactory([CanBeNull] ILoggerFactory loggerFactory)
      {
         _loggerFactory = loggerFactory;
      }

      [NotNull]
      private T CreateContext()
      {
         var optionsBuilder = CreateOptionsBuilder(_connectionString);
         var ctx = CreateContext(optionsBuilder.Options, new DbContextSchema(Schema));
         RunMigrations(ctx);

         if (_useSharedTables)
            _tx = ctx.Database.BeginTransaction(IsolationLevel.ReadCommitted);

         return ctx;
      }

      /// <summary>
      /// Runs migrations for provided <paramref name="ctx" />.
      /// </summary>
      /// <param name="ctx">Database context to run migrations for.</param>
      /// <exception cref="ArgumentNullException">The provided context is <c>null</c>.</exception>
      protected virtual void RunMigrations([NotNull] T ctx)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         // concurrent execution is not supported by EF migrations
         lock (_locks.GetOrAdd(Schema, s => new object()))
         {
            ctx.Database.Migrate();
         }
      }

      /// <summary>
      /// Creates and configures the <see cref="DbContextOptionsBuilder{TContext}"/>
      /// </summary>
      /// <param name="connString">Database connection string</param>
      /// <returns>An instance of <see cref="DbContextOptionsBuilder{TContext}"/></returns>
      /// <exception cref="ArgumentNullException"><paramref name="connString"/> is null.</exception>
      [NotNull]
      protected virtual DbContextOptionsBuilder<T> CreateOptionsBuilder([NotNull] string connString)
      {
         if (connString == null)
            throw new ArgumentNullException(nameof(connString));

         var builder = new DbContextOptionsBuilder<T>()
                       .UseSqlServer(connString, ConfigureSqlServer)
                       .AddSchemaAwareSqlServerComponents();

         if (_loggerFactory != null)
            builder.UseLoggerFactory(_loggerFactory);

         return builder;
      }

      /// <summary>
      /// Configures SQL Server options.
      /// </summary>
      /// <param name="builder">A builder for configuration of the options.</param>
      /// <exception cref="ArgumentNullException">The <paramref name="builder"/> is null.</exception>
      protected virtual void ConfigureSqlServer([NotNull] SqlServerDbContextOptionsBuilder builder)
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         builder.MigrationsHistoryTable(_HISTORY_TABLE_NAME, Schema);
      }

      /// <summary>
      /// Rollbacks transaction if shared tables are used
      /// otherwise the migrations are rolled back and all tables, functions, views and the newly generated schema are deleted.
      /// </summary>
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

         ctx.Database.ExecuteSqlCommand(GetSqlForCleanup(schema));
         ctx.Database.ExecuteSqlCommand(GetDropSchemaSql(schema));
      }

      [NotNull]
      private static string GetDropSchemaSql(string schema)
      {
         return $"DROP SCHEMA [{schema}]";
      }

      [NotNull]
      private static string GetSqlForCleanup(string schema)
      {
         return $@"
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
";
      }
   }
}
