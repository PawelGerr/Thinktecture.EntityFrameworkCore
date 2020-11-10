namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Factory for creation of <see cref="ITenantDatabaseProvider"/>.
   /// </summary>
   public interface ITenantDatabaseProviderFactory
   {
      /// <summary>
      /// Creates an instance of <see cref="ITenantDatabaseProvider"/>.
      /// </summary>
      /// <returns>An instance of <see cref="ITenantDatabaseProvider"/>.</returns>
      ITenantDatabaseProvider Create();
   }
}
