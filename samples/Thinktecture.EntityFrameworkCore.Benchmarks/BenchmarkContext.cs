using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Thinktecture.Database;

namespace Thinktecture;

public class BenchmarkContext : IDisposable
{
   public ServiceProvider RootServiceProvider { get; }

   public BenchmarkContext()
   {
      var config = GetConfiguration();

      var services = new ServiceCollection()
                     .AddDbContext<SqliteBenchmarkDbContext>(builder => builder.UseSqlite(config.GetConnectionString("sqlite"),
                                                                                          optionsBuilder => optionsBuilder
                                                                                             .AddBulkOperationSupport())
                                                                               .UseLoggerFactory(NullLoggerFactory.Instance)
                                                            )
                     .AddDbContext<SqlServerBenchmarkDbContext>(builder => builder.UseSqlServer(config.GetConnectionString("sqlServer"),
                                                                                                optionsBuilder => optionsBuilder
                                                                                                                  .AddBulkOperationSupport()
                                                                                                                  .AddCollectionParameterSupport())
                                                                                  .UseLoggerFactory(NullLoggerFactory.Instance)
                                                               );

      RootServiceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
   }

   private static IConfiguration GetConfiguration()
   {
      return new ConfigurationBuilder()
             .AddJsonFile("appsettings.json")
             .Build();
   }

   public void Dispose()
   {
      RootServiceProvider.Dispose();
   }
}
