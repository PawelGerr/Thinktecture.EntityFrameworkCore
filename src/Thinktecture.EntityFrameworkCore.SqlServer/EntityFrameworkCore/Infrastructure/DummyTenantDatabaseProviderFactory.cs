using Thinktecture.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   // ReSharper disable once ClassNeverInstantiated.Global
   internal sealed class DummyTenantDatabaseProviderFactory : ITenantDatabaseProviderFactory
   {
      /// <inheritdoc />
      public ITenantDatabaseProvider Create()
      {
         return new DummyTenantDatabaseProvider();
      }

      private class DummyTenantDatabaseProvider : ITenantDatabaseProvider
      {
         /// <inheritdoc />
         public string? Tenant => null;

         /// <inheritdoc />
         public string? GetDatabaseName(string? schema, string table)
         {
            return null;
         }
      }
   }
}
