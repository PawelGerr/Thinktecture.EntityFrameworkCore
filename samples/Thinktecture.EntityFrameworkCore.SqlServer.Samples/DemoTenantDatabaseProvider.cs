using Thinktecture.EntityFrameworkCore.Query;

namespace Thinktecture;

public class DemoTenantDatabaseProvider : ITenantDatabaseProvider
{
   public string? Tenant { get; }

   public DemoTenantDatabaseProvider(string? tenant)
   {
      Tenant = tenant;
   }

   /// <inheritdoc />
   public string? GetDatabaseName(string? schema, string table)
   {
      if (Tenant == "1")
         return "demo";

      if (Tenant == "2")
         return "demo2";

      return null;
   }
}
