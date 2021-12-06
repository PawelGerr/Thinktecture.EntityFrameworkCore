using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;
using IsolationLevel = System.Data.IsolationLevel;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// A base class for integration tests using EF Core along with SQL Server.
/// </summary>
/// <typeparam name="T">Type of the database context.</typeparam>
// ReSharper disable once UnusedMember.Global
public abstract class SqlServerDbContextIntegrationTests<T> : IDbContextFactory<T>, IDisposable
   where T : DbContext
{
   private const string _HISTORY_TABLE_NAME = "__EFMigrationsHistory";

   // ReSharper disable once StaticMemberInGenericType because the locks are all for the same database context but for different schemas.
   private static readonly ConcurrentDictionary<string, object> _locks = new(StringComparer.OrdinalIgnoreCase);

   private readonly string _connectionString;
   private readonly bool _useSharedTables;
   private readonly IMigrationExecutionStrategy _migrationExecutionStrategy;

   private string? _schema;
   private T? _arrangeDbContext;
   private T? _actDbContext;
   private T? _assertDbContext;
   private DbConnection? _masterConnection;
   private IDbContextTransaction? _tx;

   /// <summary>
   /// Logger factory.
   /// </summary>
   protected ILoggerFactory? LoggerFactory { get; private set; }

   /// <summary>
   /// Database schema in use.
   /// </summary>
   // ReSharper disable once MemberCanBePrivate.Global
   protected string Schema => _schema ??= DetermineSchema(_useSharedTables);

   /// <summary>
   /// Database context for setting up the test data.
   /// </summary>
   protected T ArrangeDbContext => _arrangeDbContext ??= CreateDbContext(true);

   /// <summary>
   /// Database context for the actual test.
   /// </summary>
   protected T ActDbContext => _actDbContext ??= CreateDbContext(true);

   /// <summary>
   /// Database context for making assertions.
   /// </summary>
   protected T AssertDbContext => _assertDbContext ??= CreateDbContext(true);

   /// <summary>
   /// Indication whether the EF model cache should be disabled or not.
   /// </summary>
   protected bool DisableModelCache { get; set; }

   /// <summary>
   /// Enables/disables sensitive data logging.
   /// </summary>
   public bool EnableSensitiveDataLogging { get; set; } = true;

   /// <summary>
   /// Indication whether the <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/> should be used or not.
   /// </summary>
   protected bool UseThinktectureSqlServerMigrationsSqlGenerator { get; set; } = true;

   /// <summary>
   /// Initializes a new instance of <see cref="SqlServerDbContextIntegrationTests{T}"/>
   /// </summary>
   /// <param name="connectionString">Connection string to use.</param>
   /// <param name="useSharedTables">Indication whether new tables with a new schema should be created or not.</param>
   /// <param name="migrationExecutionStrategy">
   /// Migrates the database.
   /// Default is <see cref="MigrationExecutionStrategies.Migrations"/>.
   /// </param>
   protected SqlServerDbContextIntegrationTests(string connectionString,
                                                bool useSharedTables = true,
                                                IMigrationExecutionStrategy? migrationExecutionStrategy = null)
   {
      _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
      _useSharedTables = useSharedTables;
      _migrationExecutionStrategy = migrationExecutionStrategy ?? MigrationExecutionStrategies.Migrations;
   }

   /// <summary>
   /// Creates a new instance of the database context.
   /// </summary>
   /// <param name="options">Options to use for creation.</param>
   /// <param name="schema">Database schema to use.</param>
   /// <returns>A new instance of the database context.</returns>
   protected virtual T CreateContext(DbContextOptions<T> options, IDbDefaultSchema schema)
   {
      var instance = Activator.CreateInstance(typeof(T), options, schema) ?? throw new Exception($"Could not create an instance of type of '{typeof(T).ShortDisplayName()}'");
      return (T)instance;
   }

   /// <summary>
   /// Gets/generates schema to be used.
   /// </summary>
   /// <param name="useSharedTables">Indication whether a new schema should be generated or a shared one.</param>
   /// <returns>A database schema.</returns>
   protected virtual string DetermineSchema(bool useSharedTables)
   {
      return useSharedTables ? "tests" : Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
   }

   /// <summary>
   /// Sets or resets the logger factory to be used by EF.
   /// </summary>
   /// <param name="loggerFactory">Logger factory to use.</param>
   // ReSharper disable once UnusedMember.Global
   protected void UseLoggerFactory(ILoggerFactory? loggerFactory)
   {
      LoggerFactory = loggerFactory;
   }

   /// <inheritdoc />
   public T CreateDbContext()
   {
      return CreateDbContext(false);
   }

   /// <summary>
   /// Creates a new <see cref="DbContext"/>.
   /// </summary>
   /// <param name="useMasterConnection">
   /// Indication whether to use the master connection or a new one.
   /// </param>
   /// <returns>A new instance of <typeparamref name="T"/>.</returns>
   public T CreateDbContext(bool useMasterConnection)
   {
      if (!useMasterConnection && _useSharedTables)
         throw new NotSupportedException($"A database transaction cannot be shared among different connections. Set 'useSharedTables' to 'false' or use '{nameof(ArrangeDbContext)}/{nameof(ActDbContext)}/{nameof(AssertDbContext)}' which use the same database connection.");

      bool isFirstCtx;

      lock (_locks.GetOrAdd(Schema, _ => new object()))
      {
         isFirstCtx = _masterConnection == null;
         _masterConnection ??= CreateConnection(_connectionString);
      }

      var optionsBuilder = useMasterConnection
                              ? CreateOptionsBuilder(_masterConnection)
                              : CreateOptionsBuilder(null);

      var ctx = CreateContext(optionsBuilder.Options, new DbDefaultSchema(Schema));

      if (isFirstCtx)
      {
         RunMigrations(ctx);

         if (_useSharedTables)
            _tx = BeginTransaction(ctx);
      }
      else if (_tx != null)
      {
         ctx.Database.UseTransaction(_tx.GetDbTransaction());
      }

      return ctx;
   }

   /// <summary>
   /// Creates a new <see cref="DbConnection"/>.
   /// </summary>
   /// <param name="connectionString">Connection string.</param>
   /// <returns>A database connection.</returns>
   protected virtual DbConnection CreateConnection(string connectionString)
   {
      return new SqlConnection(connectionString);
   }

   /// <summary>
   /// Starts a new transaction.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <returns>An instance of <see cref="IDbContextTransaction"/>.</returns>
   protected virtual IDbContextTransaction BeginTransaction(T ctx)
   {
      if (ctx == null)
         throw new ArgumentNullException(nameof(ctx));

      return ctx.Database.BeginTransaction(IsolationLevel.ReadCommitted);
   }

   /// <summary>
   /// Runs migrations for provided <paramref name="ctx" />.
   /// </summary>
   /// <param name="ctx">Database context to run migrations for.</param>
   /// <exception cref="ArgumentNullException">The provided context is <c>null</c>.</exception>
   protected virtual void RunMigrations(T ctx)
   {
      if (ctx == null)
         throw new ArgumentNullException(nameof(ctx));

      // concurrent execution is not supported by EF migrations
      lock (_locks.GetOrAdd(Schema, _ => new object()))
      {
         _migrationExecutionStrategy.Migrate(ctx);
      }
   }

   /// <summary>
   /// Creates and configures the <see cref="DbContextOptionsBuilder{TContext}"/>
   /// </summary>
   /// <param name="connection">Database connection to use.</param>
   /// <returns>An instance of <see cref="DbContextOptionsBuilder{TContext}"/></returns>
   /// <exception cref="ArgumentNullException"><paramref name="connection"/> is null.</exception>
   protected virtual DbContextOptionsBuilder<T> CreateOptionsBuilder(DbConnection? connection)
   {
      var builder = new DbContextOptionsBuilder<T>();

      if (connection is null)
      {
         builder.UseSqlServer(_connectionString, ConfigureSqlServer);
      }
      else
      {
         builder.UseSqlServer(connection, ConfigureSqlServer);
      }

      builder.AddSchemaRespectingComponents();

      if (DisableModelCache)
         builder.ReplaceService<IModelCacheKeyFactory, CachePerContextModelCacheKeyFactory>();

      if (EnableSensitiveDataLogging)
         builder.EnableSensitiveDataLogging();

      if (LoggerFactory != null)
         builder.UseLoggerFactory(LoggerFactory);

      return builder;
   }

   /// <summary>
   /// Configures SQL Server options.
   /// </summary>
   /// <param name="builder">A builder for configuration of the options.</param>
   /// <exception cref="ArgumentNullException">The <paramref name="builder"/> is null.</exception>
   protected virtual void ConfigureSqlServer(SqlServerDbContextOptionsBuilder builder)
   {
      if (builder == null)
         throw new ArgumentNullException(nameof(builder));

      builder.MigrationsHistoryTable(_HISTORY_TABLE_NAME, Schema);

      if (UseThinktectureSqlServerMigrationsSqlGenerator)
         builder.UseThinktectureSqlServerMigrationsSqlGenerator();
   }

   /// <summary>
   /// Rollbacks transaction if shared tables are used
   /// otherwise the migrations are rolled back and all tables, functions, views and the newly generated schema are deleted.
   /// </summary>
   public void Dispose()
   {
      Dispose(true);
      GC.SuppressFinalize(this);
   }

   /// <summary>
   /// Disposes of inner resources.
   /// </summary>
   /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqlServerDbContextIntegrationTests{T}.Dispose()"/>.</param>
   protected virtual void Dispose(bool disposing)
   {
      if (!disposing)
         return;

      if (_masterConnection == null)
         return;

      if (_useSharedTables)
      {
         _tx?.Rollback();
         _tx?.Dispose();
      }
      else
      {
         var ctx = _actDbContext ?? _arrangeDbContext ?? _assertDbContext;

         if (ctx != null)
         {
            RollbackMigrations(ctx);
            CleanUpDatabase(ctx, Schema);
         }
      }

      _arrangeDbContext?.Dispose();
      _actDbContext?.Dispose();
      _assertDbContext?.Dispose();
      _masterConnection?.Dispose();

      _arrangeDbContext = null;
      _actDbContext = null;
      _assertDbContext = null;
      _masterConnection = null;
   }

   private static void RollbackMigrations(T ctx)
   {
      if (ctx == null)
         throw new ArgumentNullException(nameof(ctx));

      ctx.GetService<IMigrator>().Migrate("0");
   }

   private static void CleanUpDatabase(T ctx, string schema)
   {
      if (ctx == null)
         throw new ArgumentNullException(nameof(ctx));
      if (schema == null)
         throw new ArgumentNullException(nameof(schema));

      var sqlHelper = ctx.GetService<ISqlGenerationHelper>();

      ctx.Database.ExecuteSqlRaw(GetSqlForCleanup(), new SqlParameter("@schema", schema));
      ctx.Database.ExecuteSqlRaw(GetDropSchemaSql(sqlHelper, schema));
   }

   private static string GetDropSchemaSql(ISqlGenerationHelper sqlHelper, string schema)
   {
      if (sqlHelper == null)
         throw new ArgumentNullException(nameof(sqlHelper));

      return $"DROP SCHEMA {sqlHelper.DelimitIdentifier(schema)}";
   }

   private static string GetSqlForCleanup()
   {
      return @"
DECLARE @crlf NVARCHAR(MAX) = CHAR(13) + CHAR(10);
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
