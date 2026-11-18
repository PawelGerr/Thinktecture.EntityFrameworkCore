using System.Reflection;
using System.Runtime.CompilerServices;

namespace Thinktecture.Internal;

public class BulkOperationsSqlExecutorTests
{
   [Fact]
   public void Should_have_no_extension_methods()
   {
      typeof(BulkOperationsSqlExecutor).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                       .Where(method => method.IsDefined(typeof(ExtensionAttribute), false))
                                       .Should().BeEmpty();
   }
}
