using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <inheritdoc />
   [SuppressMessage("ReSharper", "EF1001")]
   public class ThinktectureRelationalQueryContextFactory<TFactory> : IQueryContextFactory
      where TFactory : IQueryContextFactory
   {
      private const string _TENANT_PARAM_PREFIX = "97B894F9-5CDA-4B43-B7AC-6BCCE58FC19E";

      private readonly TFactory _factory;
      private readonly ITenantDatabaseProviderFactory _tenantDatabaseProviderFactory;

      /// <summary>
      /// Initializes new instance of <see cref="ThinktectureRelationalQueryContextFactory{T}"/>.
      /// </summary>
      /// <param name="factory">Inner factory.</param>
      /// <param name="tenantDatabaseProviderFactory">Tenant provider.</param>
      public ThinktectureRelationalQueryContextFactory(
         TFactory factory,
         ITenantDatabaseProviderFactory tenantDatabaseProviderFactory)
      {
         _factory = factory ?? throw new ArgumentNullException(nameof(factory));
         _tenantDatabaseProviderFactory = tenantDatabaseProviderFactory ?? throw new ArgumentNullException(nameof(tenantDatabaseProviderFactory));
      }

      /// <inheritdoc />
      public QueryContext Create()
      {
         var ctx = _factory.Create();

         AddTenantParameter(ctx);

         return ctx;
      }

      /// <summary>
      /// Adds a parameter so EF query cache treats queries for different tenants as different queries.
      /// </summary>
      /// <param name="ctx">Query context.</param>
      private void AddTenantParameter(IParameterValues ctx)
      {
         var tenantDatabaseProvider = _tenantDatabaseProviderFactory.Create();
         ctx.AddParameter($"{_TENANT_PARAM_PREFIX}|{tenantDatabaseProvider.Tenant}", null);
      }
   }
}
