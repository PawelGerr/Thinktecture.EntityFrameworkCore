using System;
using Moq;
using Thinktecture.EntityFrameworkCore.Query;

namespace Thinktecture
{
   public class TestTenantDatabaseProviderFactory : ITenantDatabaseProviderFactory
   {
      private readonly Mock<ITenantDatabaseProvider> _tenantDatabaseProviderMock;

      public TestTenantDatabaseProviderFactory(Mock<ITenantDatabaseProvider> tenantDatabaseProviderMock)
      {
         _tenantDatabaseProviderMock = tenantDatabaseProviderMock ?? throw new ArgumentNullException(nameof(tenantDatabaseProviderMock));
      }

      public ITenantDatabaseProvider Create()
      {
         return _tenantDatabaseProviderMock.Object;
      }
   }
}
