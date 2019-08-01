using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;
using Xunit.Abstractions;

[assembly: SuppressMessage("ReSharper", "CA1063")]
[assembly: SuppressMessage("ReSharper", "CA1816")]
[assembly: SuppressMessage("ReSharper", "CA1822")]

namespace Thinktecture
{
   public class TestBase : IDisposable
   {
      private readonly SqliteConnection _connection;

      protected DbContextOptionsBuilder<DbContextWithSchema> OptionBuilder { get; }
      private DbContextWithSchema _ctx;

      protected LoggingLevelSwitch LogLevelSwitch { get; }

      // use different schemas because EF Core uses static cache
      private string _schema;
      private bool _isSchemaSet;

      protected string Schema
      {
         get
         {
            if (!_isSchemaSet && _schema == null)
               _schema = Guid.NewGuid().ToString();

            return _schema;
         }
         set
         {
            _schema = value;
            _isSchemaSet = true;
         }
      }

      [NotNull]
      protected DbContextWithSchema DbContextWithSchema => _ctx ?? (_ctx = new DbContextWithSchema(OptionBuilder.Options, Schema));

      protected TestBase([NotNull] ITestOutputHelper testOutputHelper)
      {
         _connection = new SqliteConnection("DataSource=:memory:");
         _connection.Open();

         LogLevelSwitch = new LoggingLevelSwitch();
         var loggerFactory = CreateLoggerFactory(testOutputHelper, LogLevelSwitch);

         OptionBuilder = new DbContextOptionsBuilder<DbContextWithSchema>()
                         .UseSqlite(_connection)
                         .UseLoggerFactory(loggerFactory)
                         .ReplaceService<IModelCacheKeyFactory, DbSchemaAwareModelCacheKeyFactory>();
      }

      private ILoggerFactory CreateLoggerFactory([NotNull] ITestOutputHelper testOutputHelper, LoggingLevelSwitch loggingLevelSwitch)
      {
         if (testOutputHelper == null)
            throw new ArgumentNullException(nameof(testOutputHelper));

         var loggerConfig = new LoggerConfiguration()
                            .MinimumLevel.ControlledBy(loggingLevelSwitch)
                            .WriteTo.TestOutput(testOutputHelper, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

         return new LoggerFactory()
            .AddSerilog(loggerConfig.CreateLogger());
      }

      [NotNull]
      protected static DbContextWithSchema CreateContextWithSchema(string schema)
      {
         var options = new DbContextOptionsBuilder<DbContextWithSchema>().Options;
         return new DbContextWithSchema(options, schema);
      }

      [NotNull]
      protected DbContextWithoutSchema CreateContextWithoutSchema()
      {
         var options = new DbContextOptionsBuilder<DbContextWithoutSchema>().Options;
         return new DbContextWithoutSchema(options);
      }

      public virtual void Dispose()
      {
         _ctx?.Dispose();
         _connection.Dispose();
      }
   }
}
