using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <inheritdoc />
   [SuppressMessage("ReSharper", "EF1001")]
   public class ThinktectureRelationalQueryContextFactory : RelationalQueryContextFactory
   {
      private const string _TENANT_PARAM_PREFIX = "97B894F9-5CDA-4B43-B7AC-6BCCE58FC19E";

      private readonly ITenantDatabaseProviderFactory _tenantDatabaseProviderFactory;

      /// <inheritdoc />
      public ThinktectureRelationalQueryContextFactory(
         QueryContextDependencies dependencies,
         RelationalQueryContextDependencies relationalDependencies,
         ITenantDatabaseProviderFactory tenantDatabaseProviderFactory)
         : base(dependencies, relationalDependencies)
      {
         _tenantDatabaseProviderFactory = tenantDatabaseProviderFactory ?? throw new ArgumentNullException(nameof(tenantDatabaseProviderFactory));
      }

      /// <inheritdoc />
      public override QueryContext Create()
      {
         var ctx = base.Create();

         AddTenantParameter(ctx);

         return ctx;
      }

      /// <summary>
      /// Adds a parameter so EF query cache treats queries for different tenants as different queries.
      /// </summary>
      /// <param name="ctx"></param>
      private void AddTenantParameter(IParameterValues ctx)
      {
         var tenantDatabaseProvider = _tenantDatabaseProviderFactory.Create();
         ctx.AddParameter($"{_TENANT_PARAM_PREFIX}|{tenantDatabaseProvider.Tenant}", null);
      }
   }
}
