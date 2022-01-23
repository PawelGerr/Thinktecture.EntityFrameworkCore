using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.Logging;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Provides instances of <see cref="DbContext"/> for testing purposes.
/// </summary>
/// <typeparam name="T">Type of the database context.</typeparam>
public class SqlServerTestDbContextProvider<T> : ITestDbContextProvider<T>
   where T : DbContext
{
   // ReSharper disable once StaticMemberInGenericType because the locks are all for the same database context but for different schemas.
   private static readonly ConcurrentDictionary<string, object> _locks = new(StringComparer.OrdinalIgnoreCase);

   private readonly bool _isUsingSharedTables;
   private readonly DbContextOptions<T> _masterDbContextOptions;
   private readonly DbContextOptions<T> _dbContextOptions;
   private readonly IMigrationExecutionStrategy _migrationExecutionStrategy;
   private readonly DbConnection _masterConnection;
   private readonly IReadOnlyList<Action<T>> _contextInitializations;
   private readonly Func<DbContextOptions<T>, IDbDefaultSchema, T?>? _contextFactory;
   private readonly TestingLoggingOptions _testingLoggingOptions;

   private T? _arrangeDbContext;
   private T? _actDbContext;
   private T? _assertDbContext;
   private IDbContextTransaction? _tx;
   private bool _isAtLeastOneContextCreated;
   private readonly IsolationLevel _sharedTablesIsolationLevel;

   /// <inheritdoc />
   public T ArrangeDbContext => _arrangeDbContext ??= CreateDbContext(true);

   /// <inheritdoc />
   public T ActDbContext => _actDbContext ??= CreateDbContext(true);

   /// <inheritdoc />
   public T AssertDbContext => _assertDbContext ??= CreateDbContext(true);

   /// <summary>
   /// Database schema to use.
   /// </summary>
   // ReSharper disable once MemberCanBePrivate.Global
   public string Schema { get; }

   /// <summary>
   /// Contains executed commands if this feature was activated.
   /// </summary>
   public IReadOnlyCollection<string>? ExecutedCommands { get; }

   /// <summary>
   /// Log level switch.
   /// </summary>
   public TestingLogLevelSwitch LogLevelSwitch => _testingLoggingOptions.LogLevelSwitch;

   /// <summary>
   /// Initializes a new instance of <see cref="SqlServerTestDbContextProvider{T}"/>
   /// </summary>
   /// <param name="options">Options.</param>
   protected internal SqlServerTestDbContextProvider(SqlServerTestDbContextProviderOptions<T> options)
   {
      ArgumentNullException.ThrowIfNull(options);

      Schema = options.Schema ?? throw new ArgumentException($"The '{nameof(options.Schema)}' cannot be null.", nameof(options));
      _sharedTablesIsolationLevel = ValidateIsolationLevel(options);
      _isUsingSharedTables = options.IsUsingSharedTables;
      _masterConnection = options.MasterConnection ?? throw new ArgumentException($"The '{nameof(options.MasterConnection)}' cannot be null.", nameof(options));
      _masterDbContextOptions = options.MasterDbContextOptions ?? throw new ArgumentException($"The '{nameof(options.MasterDbContextOptions)}' cannot be null.", nameof(options));
      _dbContextOptions = options.DbContextOptions ?? throw new ArgumentException($"The '{nameof(options.DbContextOptions)}' cannot be null.", nameof(options));
      _migrationExecutionStrategy = options.MigrationExecutionStrategy ?? throw new ArgumentException($"The '{nameof(options.MigrationExecutionStrategy)}' cannot be null.", nameof(options));
      _testingLoggingOptions = options.TestingLoggingOptions ?? throw new ArgumentException($"The '{nameof(options.TestingLoggingOptions)}' cannot be null.", nameof(options));
      _contextInitializations = options.ContextInitializations ?? throw new ArgumentException($"The '{nameof(options.ContextInitializations)}' cannot be null.", nameof(options));
      ExecutedCommands = options.ExecutedCommands;
      _contextFactory = options.ContextFactory;
   }

   private static IsolationLevel ValidateIsolationLevel(SqlServerTestDbContextProviderOptions<T> options)
   {
      if (!options.SharedTablesIsolationLevel.HasValue)
         return IsolationLevel.ReadCommitted;

      if (Enum.IsDefined(options.SharedTablesIsolationLevel.Value))
         throw new ArgumentException($"The provided isolation level '{options.SharedTablesIsolationLevel}' is invalid.");

      if (options.SharedTablesIsolationLevel < IsolationLevel.ReadCommitted)
         throw new ArgumentException($"The isolation level '{options.SharedTablesIsolationLevel}' cannot be less than '{nameof(IsolationLevel.ReadCommitted)}'.");

      return options.SharedTablesIsolationLevel.Value;
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
      if (!useMasterConnection && _isUsingSharedTables)
         throw new NotSupportedException($"A database transaction cannot be shared among different connections, so the isolation of tests couldn't be guaranteed. Set 'useMasterConnection' to 'true' or 'useSharedTables' to 'false' or use '{nameof(ArrangeDbContext)}/{nameof(ActDbContext)}/{nameof(AssertDbContext)}' which use the same database connection.");

      bool isFirstCtx;

      lock (_locks.GetOrAdd(Schema, _ => new object()))
      {
         isFirstCtx = !_isAtLeastOneContextCreated;
         _isAtLeastOneContextCreated = true;
      }

      var options = useMasterConnection ? _masterDbContextOptions : _dbContextOptions;
      var ctx = CreateDbContext(options, new DbDefaultSchema(Schema));

      foreach (var ctxInit in _contextInitializations)
      {
         ctxInit(ctx);
      }

      if (isFirstCtx)
      {
         RunMigrations(ctx);

         if (_isUsingSharedTables)
            _tx = BeginTransaction(ctx);
      }
      else if (_tx != null)
      {
         ctx.Database.UseTransaction(_tx.GetDbTransaction());
      }

      return ctx;
   }

   /// <summary>
   /// Creates a new instance of the database context.
   /// </summary>
   /// <param name="options">Options to use for creation.</param>
   /// <param name="schema">Database schema to use.</param>
   /// <returns>A new instance of the database context.</returns>
   protected virtual T CreateDbContext(DbContextOptions<T> options, IDbDefaultSchema schema)
   {
      var ctx = _contextFactory?.Invoke(options, schema);

      if (ctx is not null)
         return ctx;

      ctx = (T?)Activator.CreateInstance(typeof(T), options, schema);

      if (ctx is null)
      {
         if (!_isUsingSharedTables)
         {
            throw new Exception(@$"Could not create an instance of type of '{typeof(T).ShortDisplayName()}' using constructor parameters ({typeof(DbContextOptions<T>).ShortDisplayName()} options, {nameof(IDbDefaultSchema)} schema).
Please provide the corresponding constructor or a custom factory via '{typeof(SqlServerTestDbContextProviderBuilder<T>).ShortDisplayName()}.{nameof(SqlServerTestDbContextProviderBuilder<T>.UseContextFactory)}'.");
         }

         ctx = (T)(Activator.CreateInstance(typeof(T), options)
                   ?? throw new Exception(@$"Could not create an instance of type of '{typeof(T).ShortDisplayName()}' neither using constructor parameters ({typeof(DbContextOptions<T>).ShortDisplayName()} options, {nameof(IDbDefaultSchema)} schema) nor using construct ({typeof(DbContextOptions<T>).ShortDisplayName()} options).
Please provide the corresponding constructor or a custom factory via '{typeof(SqlServerTestDbContextProviderBuilder<T>).ShortDisplayName()}.{nameof(SqlServerTestDbContextProviderBuilder<T>.UseContextFactory)}'."));
      }

      return ctx;
   }

   /// <summary>
   /// Starts a new transaction.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <returns>An instance of <see cref="IDbContextTransaction"/>.</returns>
   protected virtual IDbContextTransaction BeginTransaction(T ctx)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      return ctx.Database.BeginTransaction(_sharedTablesIsolationLevel);
   }

   /// <summary>
   /// Runs migrations for provided <paramref name="ctx" />.
   /// </summary>
   /// <param name="ctx">Database context to run migrations for.</param>
   /// <exception cref="ArgumentNullException">The provided context is <c>null</c>.</exception>
   protected virtual void RunMigrations(T ctx)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      // concurrent execution is not supported by EF migrations
      lock (_locks.GetOrAdd(Schema, _ => new object()))
      {
         var logLevel = LogLevelSwitch.MinimumLogLevel;

         try
         {
            LogLevelSwitch.MinimumLogLevel = _testingLoggingOptions.MigrationLogLevel;

            _migrationExecutionStrategy.Migrate(ctx);
         }
         finally
         {
            LogLevelSwitch.MinimumLogLevel = logLevel;
         }
      }
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
   /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqlServerTestDbContextProvider{T}.Dispose()"/>.</param>
   protected virtual void Dispose(bool disposing)
   {
      if (!disposing)
         return;

      if (_isAtLeastOneContextCreated)
      {
         DisposeContextsAndRollbackMigrations();
         _isAtLeastOneContextCreated = false;
      }

      _masterConnection.Dispose();
      _testingLoggingOptions.Dispose();
   }

   private void DisposeContextsAndRollbackMigrations()
   {
      if (_isUsingSharedTables)
      {
         _tx?.Rollback();
         _tx?.Dispose();
      }
      else
      {
         // Create a new ctx as a last resort to rollback migrations and clean up the database
         using var ctx = _actDbContext ?? _arrangeDbContext ?? _assertDbContext ?? CreateDbContext(true);

         RollbackMigrations(ctx);
         CleanUpDatabase(ctx, Schema);
      }

      _arrangeDbContext?.Dispose();
      _actDbContext?.Dispose();
      _assertDbContext?.Dispose();

      _arrangeDbContext = null;
      _actDbContext = null;
      _assertDbContext = null;
   }

   private static void RollbackMigrations(T ctx)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      ctx.GetService<IMigrator>().Migrate("0");
   }

   private static void CleanUpDatabase(T ctx, string schema)
   {
      ArgumentNullException.ThrowIfNull(ctx);
      ArgumentNullException.ThrowIfNull(schema);

      var sqlHelper = ctx.GetService<ISqlGenerationHelper>();

      ctx.Database.ExecuteSqlRaw(GetSqlForCleanup(), new SqlParameter("@schema", schema));
      ctx.Database.ExecuteSqlRaw(GetDropSchemaSql(sqlHelper, schema));
   }

   private static string GetDropSchemaSql(ISqlGenerationHelper sqlHelper, string schema)
   {
      ArgumentNullException.ThrowIfNull(sqlHelper);

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
