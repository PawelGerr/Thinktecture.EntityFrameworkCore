namespace Thinktecture.EntityFrameworkCore.Query;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class DummyTenantDatabaseProviderFactory : ITenantDatabaseProviderFactory
{
   public static readonly DummyTenantDatabaseProviderFactory Instance = new();

   private static readonly DummyTenantDatabaseProvider _provider = new();

   /// <inheritdoc />
   public ITenantDatabaseProvider Create()
   {
      return _provider;
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