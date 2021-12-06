using Thinktecture.EntityFrameworkCore.Query;

namespace Thinktecture;

public class DemoTenantDatabaseProviderFactory : ITenantDatabaseProviderFactory
{
   /// <inheritdoc />
   public ITenantDatabaseProvider Create()
   {
      return new DemoTenantDatabaseProvider(CurrentTenant.Value);
   }
}