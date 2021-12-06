using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Base class for <see cref="IDbContextOptionsExtension"/>.
/// </summary>
[SuppressMessage("ReSharper", "EF1001")]
public abstract class DbContextOptionsExtensionBase
{
   /// <summary>
   /// Adds <see cref="IEntityDataReaderFactory"/> and its dependencies.
   /// </summary>
   /// <param name="services"></param>
   protected void AddEntityDataReader(IServiceCollection services)
   {
      services.TryAddSingleton<IEntityDataReaderFactory, EntityDataReaderFactory>();
      services.TryAddSingleton<IPropertyGetterCache, PropertyGetterCache>();
   }

   /// <summary>
   /// Gets the lifetime of a Entity Framework Core service.
   /// </summary>
   /// <typeparam name="TService">Service to fetch lifetime for.</typeparam>
   /// <returns>Lifetime of the provided service.</returns>
   /// <exception cref="InvalidOperationException">If service is not found.</exception>
   protected ServiceLifetime GetLifetime<TService>()
   {
      var serviceType = typeof(TService);

      if (EntityFrameworkRelationalServicesBuilder.RelationalServices.TryGetValue(serviceType, out var serviceCharacteristics) ||
          EntityFrameworkServicesBuilder.CoreServices.TryGetValue(serviceType, out serviceCharacteristics))
         return serviceCharacteristics.Lifetime;

      throw new InvalidOperationException($"No service characteristics for service '{serviceType.Name}' found.");
   }

   /// <summary>
   /// Adds the implementation <typeparamref name="TImplementation"/> of the type <typeparamref name="TService"/>
   /// if the <paramref name="services"/> contains a registration of type <typeparamref name="TExpectedImplementation"/>,
   /// an <see cref="InvalidOperationException"/> is thrown otherwise.
   /// </summary>
   /// <param name="services">Service collection.</param>
   /// <typeparam name="TService">Type of the service.</typeparam>
   /// <typeparam name="TImplementation">Type of the new implementation.</typeparam>
   /// <typeparam name="TExpectedImplementation">Type of the implementation expected to be in <paramref name="services"/>.</typeparam>
   /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">
   /// No registration of service type <typeparamref name="TService"/> found in <paramref name="services"/>
   /// - or the implementation type is not the <typeparamref name="TExpectedImplementation"/>.
   /// </exception>
   protected void AddWithCheck<TService, TImplementation, TExpectedImplementation>(IServiceCollection services)
      where TImplementation : TService
      where TExpectedImplementation : TService
   {
      if (services == null)
         throw new ArgumentNullException(nameof(services));

      var serviceType = typeof(TService);
      var currentDescriptor = services.LastOrDefault(d => d.ServiceType == serviceType);

      if (currentDescriptor is null)
         throw new InvalidOperationException($@"No registration of the Entity Framework Core service '{serviceType.FullName}' found. Please make sure the database provider is registered first (via 'UseSqlServer' or 'UseSqlite' etc).");

      var newImplementationType = typeof(TImplementation);
      var expectedImplementationType = typeof(TExpectedImplementation);

      if (currentDescriptor.ImplementationType != expectedImplementationType)
         throw new InvalidOperationException($@"Current registration of the Entity Framework Core service '{serviceType.FullName}' is '{currentDescriptor.ImplementationType?.FullName}' but was expected to be '{expectedImplementationType.FullName}'. Replacing current implementation with '{newImplementationType.FullName}' may lead to unexpected behavior.");

      var lifetime = GetLifetime<TService>();
      services.Add<TService, TImplementation>(lifetime);
   }
}
