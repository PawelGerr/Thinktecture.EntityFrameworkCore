using System;
using JetBrains.Annotations;
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
      private static readonly Lazy<SamplesContext> _lazy = new Lazy<SamplesContext>(CreateTestConfiguration);

      public static SamplesContext Instance => _lazy.Value;

      public IConfiguration Configuration { get; }

      public string ConnectionString => Configuration.GetConnectionString("default");

      [NotNull]
      private static SamplesContext CreateTestConfiguration()
      {
         var config = GetConfiguration();
#pragma warning disable 618, CA2000
         var loggerFactory = new LoggerFactory().AddConsole();
#pragma warning restore 618, CA2000

         return new SamplesContext(config, loggerFactory);
      }

      public SamplesContext([NotNull] IConfiguration config, [NotNull] ILoggerFactory loggerFactory)
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

      public IServiceProvider CreateServiceProvider([CanBeNull] string schema = null)
      {
         var services = new ServiceCollection()
            .AddDbContext<DemoDbContext>(builder => builder
                                                    .UseSqlServer(ConnectionString, sqlOptions =>
                                                                                    {
                                                                                       if (schema != null)
                                                                                          sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schema);

                                                                                       sqlOptions.AddRowNumberSupport()
                                                                                                 .AddTempTableSupport()
                                                                                                 .UseThinktectureSqlServerMigrationsSqlGenerator();
                                                                                    })
                                                    .UseLoggerFactory(_loggerFactory)
                                                    .AddSchemaRespectingComponents());

         if (schema != null)
            services.AddSingleton<IDbDefaultSchema>(new DbDefaultSchema(schema));

         return services.BuildServiceProvider();
      }
   }
}
