using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Thinktecture.Database;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture
{
   public class SamplesContext
   {
      private readonly ILoggerFactory _loggerFactory;
      private static readonly Lazy<SamplesContext> _lazy = new(CreateTestConfiguration);

      public static SamplesContext Instance => _lazy.Value;

      public IConfiguration Configuration { get; }

      public string ConnectionString => Configuration.GetConnectionString("default");

      private static SamplesContext CreateTestConfiguration()
      {
         var config = GetConfiguration();
         var loggerFactory = new LoggerBuilder().AddConsole().Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();

         return new SamplesContext(config, loggerFactory);
      }

      public SamplesContext(IConfiguration config, ILoggerFactory loggerFactory)
      {
         _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
         Configuration = config ?? throw new ArgumentNullException(nameof(config));
      }

      private static IConfiguration GetConfiguration()
      {
         return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
      }

      public IServiceProvider CreateServiceProvider(string? schema = null)
      {
         var services = new ServiceCollection()
            .AddDbContext<DemoDbContext>(builder => builder
                                                    .UseSqlServer(ConnectionString, sqlOptions =>
                                                                                    {
                                                                                       if (schema != null)
                                                                                          sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schema);

                                                                                       sqlOptions.AddRowNumberSupport()
                                                                                                 .AddTenantDatabaseSupport<DemoTenantDatabaseProviderFactory>()
                                                                                                 .AddTempTableSupport()
                                                                                                 .UseThinktectureSqlServerMigrationsSqlGenerator();
                                                                                    })
                                                    .EnableSensitiveDataLogging()
                                                    .UseLoggerFactory(_loggerFactory)
                                                    .AddSchemaRespectingComponents()
                                                    .AddNestedTransactionSupport());

         if (schema != null)
            services.AddSingleton<IDbDefaultSchema>(new DbDefaultSchema(schema));

         return services.BuildServiceProvider();
      }

      private class LoggerBuilder : ILoggingBuilder
      {
         public IServiceCollection Services { get; }

         public LoggerBuilder()
         {
            Services = new ServiceCollection()
               .AddLogging();
         }
      }
   }
}
