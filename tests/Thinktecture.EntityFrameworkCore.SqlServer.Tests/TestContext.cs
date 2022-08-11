using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Thinktecture;

public class TestContext
{
   private static readonly Lazy<TestContext> _lazy = new(CreateTestConfiguration);

   public static TestContext Instance => _lazy.Value;

   public IConfiguration Configuration { get; }

   public string ConnectionString => Configuration.GetConnectionString("default")
                                     ?? throw new Exception("No connection string with name 'default' found.");

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

      var loggerConfig = new LoggerConfiguration()
                         .WriteTo.TestOutput(testOutputHelper, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

      return new LoggerFactory()
         .AddSerilog(loggerConfig.CreateLogger());
   }
}
