using System;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Testing
{
   /// <summary>
   /// A base class for integration tests using EF Core along with SQLite.
   /// </summary>
   /// <typeparam name="T">Type of the database context.</typeparam>
   // ReSharper disable once UnusedMember.Global
   public abstract class SqliteDbContextIntegrationTests<T> : IDisposable
      where T : DbContext
   {
      // ReSharper disable once StaticMemberInGenericType because the lock are all for the same database context.
      private static readonly object _lock = new object();

      [NotNull]
      private readonly string _connectionString;

      private readonly IMigrationExecutionStrategy _migrationExecutionStrategy;

      private T _arrangeDbContext;
      private T _actDbContext;
      private T _assertDbContext;
      private DbConnection _dbConnection;
      private DbContextOptionsBuilder<T> _optionsBuilder;
      private ILoggerFactory _loggerFactory;

      /// <summary>
      /// Database context for setting up the test data.
      /// </summary>
      [NotNull]
      protected T ArrangeDbContext => _arrangeDbContext ?? (_arrangeDbContext = CreateContext());

      /// <summary>
      /// Database context for the actual test.
      /// </summary>
      [NotNull]
      protected T ActDbContext => _actDbContext ?? (_actDbContext = CreateContext());

      /// <summary>
      /// Database context for making assertions.
      /// </summary>
      [NotNull]
      protected T AssertDbContext => _assertDbContext ?? (_assertDbContext = CreateContext());

      /// <summary>
      /// Indication whether the EF model cache should be disabled or not.
      /// </summary>
      protected bool DisableModelCache { get; set; }

      /// <summary>
      /// Initializes a new instance of <see cref="SqliteDbContextIntegrationTests{T}"/>
      /// </summary>
      /// <param name="migrationExecutionStrategy">Migrates the database.</param>
      protected SqliteDbContextIntegrationTests([CanBeNull] IMigrationExecutionStrategy migrationExecutionStrategy = null)
         : this("DataSource=:memory:", migrationExecutionStrategy)
      {
      }

      /// <summary>
      /// Initializes a new instance of <see cref="SqliteDbContextIntegrationTests{T}"/>
      /// </summary>
      /// <param name="connectionString">Connection string.</param>
      /// <param name="migrationExecutionStrategy">Migrates the database.</param>
      protected SqliteDbContextIntegrationTests([NotNull] string connectionString,
                                                [CanBeNull] IMigrationExecutionStrategy migrationExecutionStrategy = null)
      {
         _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
         _migrationExecutionStrategy = migrationExecutionStrategy ?? MigrationExecutionStrategies.Migration;
      }

      /// <summary>
      /// Creates a new instance of the database context.
      /// </summary>
      /// <param name="options">Options to use for creation.</param>
      /// <returns>A new instance of the database context.</returns>
      [NotNull]
      protected virtual T CreateContext([NotNull] DbContextOptions<T> options)
      {
         return (T)Activator.CreateInstance(typeof(T), options);
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
         var isFirstCtx = _dbConnection == null;

         if (isFirstCtx)
         {
            _dbConnection = CreateConnection(_connectionString);
            _optionsBuilder = CreateOptionsBuilder(_dbConnection);
         }

         var ctx = CreateContext(_optionsBuilder.Options);

         if (isFirstCtx)
            RunMigrations(ctx);

         return ctx;
      }

      /// <summary>
      /// Creates a new <see cref="DbConnection"/>.
      /// </summary>
      /// <param name="connectionString">Connection string.</param>
      /// <returns>A database connection.</returns>
      [NotNull]
      protected virtual DbConnection CreateConnection([NotNull] string connectionString)
      {
         var connection = new SqliteConnection(connectionString);
         connection.Open();

         return connection;
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
         lock (_lock)
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
      [NotNull]
      protected virtual DbContextOptionsBuilder<T> CreateOptionsBuilder([NotNull] DbConnection connection)
      {
         if (connection == null)
            throw new ArgumentNullException(nameof(connection));

         var builder = new DbContextOptionsBuilder<T>()
            .UseSqlite(connection, ConfigureSqlite);

         if (DisableModelCache)
            builder.ReplaceService<IModelCacheKeyFactory, CachePerContextModelCacheKeyFactory>();

         if (_loggerFactory != null)
            builder.UseLoggerFactory(_loggerFactory);

         return builder;
      }

      /// <summary>
      /// Configures SQLite options.
      /// </summary>
      /// <param name="builder">A builder for configuration of the options.</param>
      /// <exception cref="ArgumentNullException">The <paramref name="builder"/> is null.</exception>
      protected virtual void ConfigureSqlite([NotNull] SqliteDbContextOptionsBuilder builder)
      {
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
      /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqliteDbContextIntegrationTests{T}.Dispose()"/>.</param>
      protected virtual void Dispose(bool disposing)
      {
         if (!disposing)
            return;

         if (_dbConnection == null)
            return;

         _arrangeDbContext?.Dispose();
         _actDbContext?.Dispose();
         _assertDbContext?.Dispose();
         _dbConnection?.Dispose();
      }
   }
}
