using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

[assembly: SuppressMessage("ReSharper", "CA1707")]
[assembly: SuppressMessage("ReSharper", "CA2007")]

namespace Thinktecture
{
   public class TestContext
   {
      private static readonly Lazy<TestContext> _lazy = new Lazy<TestContext>(CreateTestConfiguration);

      public static TestContext Instance => _lazy.Value;

      public IConfiguration Configuration { get; }

      public string ConnectionString => Configuration.GetConnectionString("default");

      [NotNull]
      private static TestContext CreateTestConfiguration()
      {
         var config = GetConfiguration();
         return new TestContext(config);
      }

      public TestContext([NotNull] IConfiguration config)
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
   }
}
