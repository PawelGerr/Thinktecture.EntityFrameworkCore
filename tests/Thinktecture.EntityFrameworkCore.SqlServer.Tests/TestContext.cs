using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

[assembly: SuppressMessage("ReSharper", "CA1707")]
[assembly: SuppressMessage("ReSharper", "CA2007")]

namespace Thinktecture;

public class TestContext
{
   private static readonly Lazy<TestContext> _lazy = new(CreateTestConfiguration);

   public static TestContext Instance => _lazy.Value;

   private readonly ConcurrentDictionary<ITestOutputHelper, ILoggerFactory> _loggerFactoryCache = new();

   public IConfiguration Configuration { get; }

   public string ConnectionString => Configuration.GetConnectionString("default");

   private static TestContext CreateTestConfiguration()
   {
      var config = GetConfiguration();
      return new TestContext(config);
   }

   public TestContext(IConfiguration config)
   {
      Configuration = config ?? throw new ArgumentNullException(nameof(config));
   }

   private static IConfiguration GetConfiguration()
   {
      return new ConfigurationBuilder()
             .AddJsonFile("appsettings.json")
             .AddEnvironmentVariables()
             .Build();
   }

   public ILoggerFactory GetLoggerFactory(ITestOutputHelper testOutputHelper)
   {
      ArgumentNullException.ThrowIfNull(testOutputHelper);

      return _loggerFactoryCache.GetOrAdd(testOutputHelper, helper =>
                                                            {
                                                               var loggerConfig = new LoggerConfiguration()
                                                                                  .WriteTo.TestOutput(helper, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

                                                               return new LoggerFactory()
                                                                  .AddSerilog(loggerConfig.CreateLogger());
                                                            });
   }
}
