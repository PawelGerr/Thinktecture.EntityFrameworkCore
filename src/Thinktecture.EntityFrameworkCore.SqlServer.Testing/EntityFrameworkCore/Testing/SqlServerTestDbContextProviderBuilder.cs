using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore.Migrations;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Builder for the <see cref="SqlServerTestDbContextProvider{T}"/>.
/// </summary>
/// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
public class SqlServerTestDbContextProviderBuilder<T> : TestDbContextProviderBuilder
   where T : DbContext
{
   private const string _HISTORY_TABLE_NAME = "__EFMigrationsHistory";

   private readonly string _connectionString;
   private readonly bool _useSharedTables;
   private readonly List<Action<DbContextOptionsBuilder<T>, string>> _configuresOptionsCollection;
   private readonly List<Action<SqlServerDbContextOptionsBuilder, string>> _configuresSqlServerOptionsCollection;
   private readonly List<Action<T>> _ctxInitializations;

   private bool _useThinktectureSqlServerMigrationsSqlGenerator = true;
   private string? _sharedTablesSchema;
   private Func<DbContextOptions<T>, IDbDefaultSchema, T>? _contextFactory;

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTestDbContextProviderBuilder{T}"/>.
   /// </summary>
   /// <param name="connectionString">Connection string to use.</param>
   /// <param name="useSharedTables">Indication whether to create new tables with a new schema or use the existing ones.</param>
   public SqlServerTestDbContextProviderBuilder(string connectionString, bool useSharedTables)
   {
      _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
      _useSharedTables = useSharedTables;
      _configuresOptionsCollection = new List<Action<DbContextOptionsBuilder<T>, string>>();
      _configuresSqlServerOptionsCollection = new List<Action<SqlServerDbContextOptionsBuilder, string>>();
      _ctxInitializations = new List<Action<T>>();
   }

   /// <summary>
   /// Specifies the migration strategy to use.
   /// Default is <see cref="IMigrationExecutionStrategy.Migrations"/>.
   /// </summary>
   /// <param name="migrationExecutionStrategy">Migration strategy to use.</param>
   /// <returns>Current builder for chaining</returns>
   public new SqlServerTestDbContextProviderBuilder<T> UseMigrationExecutionStrategy(IMigrationExecutionStrategy migrationExecutionStrategy)
   {
      base.UseMigrationExecutionStrategy(migrationExecutionStrategy);

      return this;
   }

   /// <summary>
   /// Sets the logger factory to be used by EF.
   /// </summary>
   /// <param name="loggerFactory">Logger factory to use.</param>
   /// <param name="enableSensitiveDataLogging">Enables or disables sensitive data logging.</param>
   /// <returns>Current builder for chaining.</returns>
   public new SqlServerTestDbContextProviderBuilder<T> UseLogging(
      ILoggerFactory? loggerFactory,
      bool enableSensitiveDataLogging = true)
   {
      base.UseLogging(loggerFactory, enableSensitiveDataLogging);

      return this;
   }

   /// <summary>
   /// Sets output helper to be used by Serilog which is passed to EF.
   /// </summary>
   /// <param name="testOutputHelper">XUnit output.</param>
   /// <param name="enableSensitiveDataLogging">Enables or disables sensitive data logging.</param>
   /// <param name="outputTemplate">The serilog output template.</param>
   /// <returns>Current builder for chaining.</returns>
   public new SqlServerTestDbContextProviderBuilder<T> UseLogging(
      ITestOutputHelper? testOutputHelper,
      bool enableSensitiveDataLogging = true,
      string? outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
   {
      base.UseLogging(testOutputHelper, enableSensitiveDataLogging, outputTemplate);

      return this;
   }

   /// <summary>
   /// Sets the log level during migrations.
   /// </summary>
   /// <param name="logLevel">Minimum log level to use during migrations.</param>
   /// <returns>Current builder for chaining.</returns>
   public new SqlServerTestDbContextProviderBuilder<T> UseMigrationLogLevel(LogLevel logLevel)
   {
      base.UseMigrationLogLevel(logLevel);

      return this;
   }

   /// <summary>
   /// Allows further configuration of the <see cref="DbContextOptionsBuilder{TContext}"/>.
   /// </summary>
   /// <param name="configure">Callback is called with the current <see cref="DbContextOptionsBuilder{TContext}"/> and the database schema.</param>
   /// <returns>Current builder for chaining.</returns>
   public SqlServerTestDbContextProviderBuilder<T> ConfigureOptions(Action<DbContextOptionsBuilder<T>, string> configure)
   {
      ArgumentNullException.ThrowIfNull(configure);

      _configuresOptionsCollection.Add(configure);

      return this;
   }

   /// <summary>
   /// Allows further configuration of the <see cref="SqlServerDbContextOptionsBuilder"/>.
   /// </summary>
   /// <param name="configure">Callback is called with the current <see cref="DbContextOptionsBuilder{TContext}"/> and the database schema.</param>
   /// <returns>Current builder for chaining.</returns>
   public SqlServerTestDbContextProviderBuilder<T> ConfigureSqlServerOptions(Action<SqlServerDbContextOptionsBuilder, string> configure)
   {
      ArgumentNullException.ThrowIfNull(configure);

      _configuresSqlServerOptionsCollection.Add(configure);

      return this;
   }

   /// <summary>
   /// Disables EF model cache.
   /// </summary>
   /// <returns>Current builder for chaining.</returns>
   public new SqlServerTestDbContextProviderBuilder<T> DisableModelCache(bool disableModelCache = true)
   {
      base.DisableModelCache(disableModelCache);

      return this;
   }

   /// <summary>
   /// Indication whether the <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/> should be used or not.
   /// </summary>
   /// <param name="useThinktectureSqlServerMigrationsSqlGenerator"></param>
   /// <returns>Current builder for chaining.</returns>
   public SqlServerTestDbContextProviderBuilder<T> UseThinktectureSqlServerMigrationsSqlGenerator(bool useThinktectureSqlServerMigrationsSqlGenerator = true)
   {
      _useThinktectureSqlServerMigrationsSqlGenerator = useThinktectureSqlServerMigrationsSqlGenerator;

      return this;
   }

   /// <summary>
   /// Indication whether collect executed commands or not.
   /// </summary>
   /// <returns>Current builder for chaining</returns>
   public new SqlServerTestDbContextProviderBuilder<T> CollectExecutedCommands(bool collectExecutedCommands = true)
   {
      base.CollectExecutedCommands(collectExecutedCommands);

      return this;
   }

   /// <summary>
   /// Changes the schema if "useSharedTables" is set to <c>true</c>.
   /// Default schema is "tests".
   /// </summary>
   /// <param name="schema">Schema to use</param>
   /// <returns>Current builder for chaining.</returns>
   public SqlServerTestDbContextProviderBuilder<T> UseSharedTableSchema(string schema)
   {
      if (String.IsNullOrWhiteSpace(schema))
         throw new ArgumentException("Schema cannot be empty.", nameof(schema));

      _sharedTablesSchema = schema;

      return this;
   }

   /// <summary>
   /// Provides a callback to execute on every creation of a new <see cref="DbContext"/>.
   /// </summary>
   /// <param name="initialize">Initialization.</param>
   /// <returns>Current builder for chaining.</returns>
   public SqlServerTestDbContextProviderBuilder<T> InitializeContext(Action<T> initialize)
   {
      ArgumentNullException.ThrowIfNull(initialize);

      _ctxInitializations.Add(initialize);

      return this;
   }

   /// <summary>
   /// Provides a custom factory to create <see cref="DbContext"/>.
   /// </summary>
   /// <param name="contextFactory">Factory to create the context of type <typeparamref name="T"/>.</param>
   /// <returns>Current builder for chaining.</returns>
   public SqlServerTestDbContextProviderBuilder<T> UseContextFactory(Func<DbContextOptions<T>, IDbDefaultSchema, T>? contextFactory)
   {
      _contextFactory = contextFactory;

      return this;
   }

   /// <summary>
   /// Gets/generates schema to be used.
   /// </summary>
   /// <param name="useSharedTables">Indication whether a new schema should be generated or a shared one.</param>
   /// <returns>A database schema.</returns>
   protected virtual string DetermineSchema(bool useSharedTables)
   {
      return useSharedTables
                ? _sharedTablesSchema ?? "tests"
                : Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
   }

   /// <summary>
   /// Creates and configures the <see cref="DbContextOptionsBuilder{TContext}"/>
   /// </summary>
   /// <param name="connection">Database connection to use.</param>
   /// <param name="schema">Database schema to use.</param>
   /// <returns>An instance of <see cref="DbContextOptionsBuilder{TContext}"/></returns>
   public virtual DbContextOptionsBuilder<T> CreateOptionsBuilder(
      DbConnection? connection,
      string schema)
   {
      var loggingOptions = CreateLoggingOptions();
      var state = new TestDbContextProviderBuilderState(loggingOptions);

      return CreateOptionsBuilder(state, connection, schema);
   }

   /// <summary>
   /// Creates and configures the <see cref="DbContextOptionsBuilder{TContext}"/>
   /// </summary>
   /// <param name="state">Current building state.</param>
   /// <param name="connection">Database connection to use.</param>
   /// <param name="schema">Database schema to use.</param>
   /// <returns>An instance of <see cref="DbContextOptionsBuilder{TContext}"/></returns>
   protected virtual DbContextOptionsBuilder<T> CreateOptionsBuilder(
      TestDbContextProviderBuilderState state,
      DbConnection? connection,
      string schema)
   {
      var builder = new DbContextOptionsBuilder<T>();

      if (connection is null)
      {
         builder.UseSqlServer(_connectionString, optionsBuilder => ConfigureSqlServer(optionsBuilder, schema));
      }
      else
      {
         builder.UseSqlServer(connection, optionsBuilder => ConfigureSqlServer(optionsBuilder, schema));
      }

      ApplyDefaultConfiguration(state, builder);

      _configuresOptionsCollection.ForEach(configure => configure(builder, schema));

      return builder;
   }

   /// <summary>
   /// Configures SQL Server options.
   /// </summary>
   /// <param name="builder">A builder for configuration of the options.</param>
   /// <param name="schema">Schema to use</param>
   /// <exception cref="ArgumentNullException">The <paramref name="builder"/> is null.</exception>
   protected virtual void ConfigureSqlServer(SqlServerDbContextOptionsBuilder builder, string schema)
   {
      ArgumentNullException.ThrowIfNull(builder);

      builder.MigrationsHistoryTable(_HISTORY_TABLE_NAME, schema);

      if (_useThinktectureSqlServerMigrationsSqlGenerator)
         builder.UseThinktectureSqlServerMigrationsSqlGenerator();

      _configuresSqlServerOptionsCollection.ForEach(configure => configure(builder, schema));
   }

   /// <summary>
   /// Builds the <see cref="SqlServerTestDbContextProvider{T}"/>.
   /// </summary>
   /// <returns>A new instance of <see cref="SqlServerTestDbContextProvider{T}"/>.</returns>
   public SqlServerTestDbContextProvider<T> Build()
   {
      // create all dependencies immediately to decouple the provider from this builder

      var schema = DetermineSchema(_useSharedTables);
      var masterConnection = new SqlConnection(_connectionString);

      try
      {
         var loggingOptions = CreateLoggingOptions();
         var state = new TestDbContextProviderBuilderState(loggingOptions);
         var masterDbContextOptions = CreateOptionsBuilder(state, masterConnection, schema).Options;
         var dbContextOptions = CreateOptionsBuilder(state, null, schema).Options;

         return new SqlServerTestDbContextProvider<T>(new SqlServerTestDbContextProviderOptions<T>(masterConnection,
                                                                                                   state.MigrationExecutionStrategy ?? IMigrationExecutionStrategy.Migrations,
                                                                                                   masterDbContextOptions,
                                                                                                   dbContextOptions,
                                                                                                   loggingOptions,
                                                                                                   _ctxInitializations.ToList(),
                                                                                                   schema)
                                                      {
                                                         IsUsingSharedTables = _useSharedTables,
                                                         ContextFactory = _contextFactory,
                                                         ExecutedCommands = state.CommandCapturingInterceptor?.Commands
                                                      });
      }
      catch
      {
         masterConnection.Dispose();
         throw;
      }
   }
}
