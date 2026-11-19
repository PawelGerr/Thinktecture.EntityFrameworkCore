using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
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
   private readonly ITestIsolationOptions _isolationOptions;
   private readonly List<Action<DbContextOptionsBuilder<T>, string?>> _configuresOptionsCollection;
   private readonly List<Action<SqlServerDbContextOptionsBuilder, string?>> _configuresSqlServerOptionsCollection;
   private readonly List<Action<T>> _ctxInitializations;

   private string? _sharedTablesSchema;

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTestDbContextProviderBuilder{T}"/>.
   /// </summary>
   /// <param name="connectionString">Connection string to use.</param>
   /// <param name="isolationOptions">Test isolation behavior.</param>
   public SqlServerTestDbContextProviderBuilder(string connectionString, ITestIsolationOptions isolationOptions)
   {
      _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
      _isolationOptions = isolationOptions;
      _configuresOptionsCollection = new List<Action<DbContextOptionsBuilder<T>, string?>>();
      _configuresSqlServerOptionsCollection = new List<Action<SqlServerDbContextOptionsBuilder, string?>>();
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
   /// Sets output helper to be used by Serilog which is passed to EF, and enables sensitive data logging.
   /// </summary>
   /// <param name="testOutputHelper">XUnit output.</param>
   /// <returns>Current builder for chaining.</returns>
   public new SqlServerTestDbContextProviderBuilder<T> UseLogging(ITestOutputHelper? testOutputHelper)
   {
      base.UseLogging(testOutputHelper);

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
   public SqlServerTestDbContextProviderBuilder<T> ConfigureOptions(Action<DbContextOptionsBuilder<T>, string?> configure)
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
   public SqlServerTestDbContextProviderBuilder<T> ConfigureSqlServerOptions(Action<SqlServerDbContextOptionsBuilder, string?> configure)
   {
      ArgumentNullException.ThrowIfNull(configure);

      _configuresSqlServerOptionsCollection.Add(configure);

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
   /// Sets the database schema of the tables.
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
   /// Creates and configures the <see cref="DbContextOptionsBuilder{TContext}"/>
   /// </summary>
   /// <param name="connection">Database connection to use.</param>
   /// <param name="schema">Default database schema to use.</param>
   /// <returns>An instance of <see cref="DbContextOptionsBuilder{TContext}"/></returns>
   public virtual DbContextOptionsBuilder<T> CreateOptionsBuilder(
      DbConnection? connection,
      string? schema)
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
   /// <param name="schema">Default database schema to use.</param>
   /// <returns>An instance of <see cref="DbContextOptionsBuilder{TContext}"/></returns>
   protected virtual DbContextOptionsBuilder<T> CreateOptionsBuilder(
      TestDbContextProviderBuilderState state,
      DbConnection? connection,
      string? schema)
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

      if (schema is not null)
         builder.AddSchemaRespectingComponents();

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
   protected virtual void ConfigureSqlServer(SqlServerDbContextOptionsBuilder builder, string? schema)
   {
      ArgumentNullException.ThrowIfNull(builder);

      builder.MigrationsHistoryTable(_HISTORY_TABLE_NAME, schema);
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

      var schema = _sharedTablesSchema;
      var masterConnection = new SqlConnection(_connectionString);

      try
      {
         var loggingOptions = CreateLoggingOptions();
         var state = new TestDbContextProviderBuilderState(loggingOptions);
         var masterDbContextOptionsBuilder = CreateOptionsBuilder(state, masterConnection, schema);
         var dbContextOptionsBuilder = CreateOptionsBuilder(state, null, schema);

         var options = new SqlServerTestDbContextProviderOptions<T>(masterConnection,
                                                                    state.MigrationExecutionStrategy ?? IMigrationExecutionStrategy.Migrations,
                                                                    masterDbContextOptionsBuilder,
                                                                    dbContextOptionsBuilder,
                                                                    loggingOptions,
                                                                    _ctxInitializations.ToList(),
                                                                    schema,
                                                                    _isolationOptions)
                       {
                          ExecutedCommands = state.CommandCapturingInterceptor?.Commands
                       };

         return new SqlServerTestDbContextProvider<T>(options);
      }
      catch
      {
         masterConnection.Dispose();
         throw;
      }
   }
}
