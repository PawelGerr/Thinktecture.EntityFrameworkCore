using Thinktecture.EntityFrameworkCore.Diagnostics;
using Thinktecture.Logging;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Contains current building state.
/// </summary>
public class TestDbContextProviderBuilderState
{
   /// <summary>
   /// Logging options.
   /// </summary>
   public TestingLoggingOptions LoggingOptions { get; }

   /// <summary>
   /// Command capturing interceptor
   /// </summary>
   public CommandCapturingInterceptor? CommandCapturingInterceptor { get; set; }

   /// <summary>
   /// Current migration execution strategy.
   /// </summary>
   public IMigrationExecutionStrategy? MigrationExecutionStrategy { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="TestDbContextProviderBuilderState"/>.
   /// </summary>
   /// <param name="loggingOptions">Logging options.</param>
   public TestDbContextProviderBuilderState(
      TestingLoggingOptions loggingOptions)
   {
      LoggingOptions = loggingOptions;
   }
}
