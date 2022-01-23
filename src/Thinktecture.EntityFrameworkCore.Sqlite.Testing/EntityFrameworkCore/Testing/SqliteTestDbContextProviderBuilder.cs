using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Builder for the <see cref="SqliteTestDbContextProvider{T}"/>.
/// </summary>
/// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
public class SqliteTestDbContextProviderBuilder<T> : TestDbContextProviderBuilder
   where T : DbContext
{
   private readonly List<Action<DbContextOptionsBuilder<T>>> _configuresOptionsCollection;
   private readonly List<Action<SqliteDbContextOptionsBuilder>> _configuresSqliteOptionsCollection;
   private readonly List<Action<T>> _ctxInitializations;

   private Func<DbContextOptions<T>, T>? _contextFactory;

   /// <summary>
   /// Initializes new instance of <see cref="SqliteTestDbContextProviderBuilder{T}"/>.
   /// </summary>
   public SqliteTestDbContextProviderBuilder()
   {
      _configuresOptionsCollection = new List<Action<DbContextOptionsBuilder<T>>>();
      _configuresSqliteOptionsCollection = new List<Action<SqliteDbContextOptionsBuilder>>();
      _ctxInitializations = new List<Action<T>>();
   }

   /// <summary>
   /// Specifies the migration strategy to use.
   /// Default is <see cref="IMigrationExecutionStrategy.Migrations"/>.
   /// </summary>
   /// <param name="migrationExecutionStrategy">Migration strategy to use.</param>
   /// <returns>Current builder for chaining</returns>
   public new SqliteTestDbContextProviderBuilder<T> UseMigrationExecutionStrategy(IMigrationExecutionStrategy migrationExecutionStrategy)
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
   public new SqliteTestDbContextProviderBuilder<T> UseLogging(
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
   public new SqliteTestDbContextProviderBuilder<T> UseLogging(
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
   public new SqliteTestDbContextProviderBuilder<T> UseMigrationLogLevel(LogLevel logLevel)
   {
      base.UseMigrationLogLevel(logLevel);

      return this;
   }

   /// <summary>
   /// Allows further configuration of the <see cref="DbContextOptionsBuilder{TContext}"/>.
   /// </summary>
   /// <param name="configure">Callback is called the current <see cref="DbContextOptionsBuilder{TContext}"/> and the database schema.</param>
   /// <returns>Current builder for chaining.</returns>
   public SqliteTestDbContextProviderBuilder<T> ConfigureOptions(Action<DbContextOptionsBuilder<T>> configure)
   {
      ArgumentNullException.ThrowIfNull(configure);

      _configuresOptionsCollection.Add(configure);

      return this;
   }

   /// <summary>
   /// Allows further configuration of the <see cref="SqliteDbContextOptionsBuilder"/>.
   /// </summary>
   /// <param name="configure">Callback is called with the current <see cref="DbContextOptionsBuilder{TContext}"/>.</param>
   /// <returns>Current builder for chaining.</returns>
   public SqliteTestDbContextProviderBuilder<T> ConfigureSqliteOptions(Action<SqliteDbContextOptionsBuilder> configure)
   {
      ArgumentNullException.ThrowIfNull(configure);

      _configuresSqliteOptionsCollection.Add(configure);

      return this;
   }

   /// <summary>
   /// Disables EF model cache.
   /// </summary>
   /// <returns>Current builder for chaining.</returns>
   public new SqliteTestDbContextProviderBuilder<T> DisableModelCache(bool disableModelCache = true)
   {
      base.DisableModelCache(disableModelCache);

      return this;
   }

   /// <summary>
   /// Indication whether collect executed commands or not.
   /// </summary>
   /// <returns>Current builder for chaining</returns>
   public new SqliteTestDbContextProviderBuilder<T> CollectExecutedCommands(bool collectExecutedCommands = true)
   {
      base.CollectExecutedCommands(collectExecutedCommands);

      return this;
   }

   /// <summary>
   /// Provides a callback to execute on every creation of a new <see cref="DbContext"/>.
   /// </summary>
   /// <param name="initialize">Initialization.</param>
   /// <returns>Current builder for chaining.</returns>
   public SqliteTestDbContextProviderBuilder<T> InitializeContext(Action<T> initialize)
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
   public SqliteTestDbContextProviderBuilder<T> UseContextFactory(Func<DbContextOptions<T>, T>? contextFactory)
   {
      _contextFactory = contextFactory;

      return this;
   }

   /// <summary>
   /// Creates and configures the <see cref="DbContextOptionsBuilder{TContext}"/>
   /// </summary>
   /// <param name="connection">Database connection to use.</param>
   /// <param name="connectionString">Connection string to use.</param>
   /// <returns>An instance of <see cref="DbContextOptionsBuilder{TContext}"/></returns>
   public virtual DbContextOptionsBuilder<T> CreateOptionsBuilder(
      DbConnection? connection,
      string connectionString)
   {
      var loggingOptions = CreateLoggingOptions();
      var state = new TestDbContextProviderBuilderState(loggingOptions);

      return CreateOptionsBuilder(state, connection, connectionString);
   }

   /// <summary>
   /// Creates and configures the <see cref="DbContextOptionsBuilder{TContext}"/>
   /// </summary>
   /// <param name="state">Current building state.</param>
   /// <param name="connection">Database connection to use.</param>
   /// <param name="connectionString">Connection string to use.</param>
   /// <returns>An instance of <see cref="DbContextOptionsBuilder{TContext}"/></returns>
   protected virtual DbContextOptionsBuilder<T> CreateOptionsBuilder(
      TestDbContextProviderBuilderState state,
      DbConnection? connection,
      string connectionString)
   {
      var builder = new DbContextOptionsBuilder<T>();

      if (connection is null)
      {
         builder.UseSqlite(connectionString, ConfigureSqlite);
      }
      else
      {
         builder.UseSqlite(connection, ConfigureSqlite);
      }

      ApplyDefaultConfiguration(state, builder);

      _configuresOptionsCollection.ForEach(configure => configure(builder));

      return builder;
   }

   /// <summary>
   /// Configures SQLite options.
   /// </summary>
   /// <param name="builder">A builder for configuration of the options.</param>
   /// <exception cref="ArgumentNullException">The <paramref name="builder"/> is null.</exception>
   protected virtual void ConfigureSqlite(SqliteDbContextOptionsBuilder builder)
   {
      ArgumentNullException.ThrowIfNull(builder);

      _configuresSqliteOptionsCollection.ForEach(configure => configure(builder));
   }

   /// <summary>
   /// Builds the <see cref="SqliteTestDbContextProvider{T}"/>.
   /// </summary>
   /// <returns>A new instance of <see cref="SqliteTestDbContextProvider{T}"/>.</returns>
   public SqliteTestDbContextProvider<T> Build()
   {
      // create all dependencies immediately to decouple the provider from this builder

      var connectionString = $"Data Source=InMemory{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
      var masterConnection = new SqliteConnection(connectionString);

      try
      {
         var loggingOptions = CreateLoggingOptions();
         var state = new TestDbContextProviderBuilderState(loggingOptions);
         var masterDbContextOptions = CreateOptionsBuilder(state, masterConnection, connectionString).Options;
         var dbContextOptions = CreateOptionsBuilder(state, null, connectionString).Options;

         return new SqliteTestDbContextProvider<T>(new SqliteTestDbContextProviderOptions<T>(masterConnection,
                                                                                             state.MigrationExecutionStrategy ?? IMigrationExecutionStrategy.Migrations,
                                                                                             masterDbContextOptions,
                                                                                             dbContextOptions,
                                                                                             loggingOptions,
                                                                                             _ctxInitializations.ToList(),
                                                                                             connectionString)
                                                   {
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
