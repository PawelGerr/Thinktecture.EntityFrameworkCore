using Thinktecture.EntityFrameworkCore.Query;

namespace Thinktecture;

public class TestTenantDatabaseProviderFactory : ITenantDatabaseProviderFactory
{
   private readonly ITenantDatabaseProvider _tenantDatabaseProviderMock;

   public TestTenantDatabaseProviderFactory(ITenantDatabaseProvider tenantDatabaseProviderMock)
   {
      _tenantDatabaseProviderMock = tenantDatabaseProviderMock ?? throw new ArgumentNullException(nameof(tenantDatabaseProviderMock));
   }

   public ITenantDatabaseProvider Create()
   {
      return _tenantDatabaseProviderMock;
   }
}
