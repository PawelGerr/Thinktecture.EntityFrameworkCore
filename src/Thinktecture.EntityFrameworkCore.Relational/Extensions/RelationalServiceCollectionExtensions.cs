using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class RelationalServiceCollectionExtensions
{
   /// <summary>
   /// Adds a service of the type specified in <typeparamref name="TService" /> with an
   /// implementation type specified in <typeparamref name="TImplementation" /> to the
   /// <paramref name="services"/> if the service type hasn't already been registered.
   /// </summary>
   /// <param name="services">The <see cref="IServiceCollection" /> to add the service to.</param>
   /// <param name="lifetime">Lifetime of the service.</param>
   /// <typeparam name="TService">The type of the service to add.</typeparam>
   /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
   /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
   public static void TryAdd<TService, TImplementation>(this IServiceCollection services, ServiceLifetime lifetime)
      where TImplementation : TService
   {
      ArgumentNullException.ThrowIfNull(services);

      services.TryAdd(ServiceDescriptor.Describe(typeof(TService), typeof(TImplementation), lifetime));
   }

   /// <summary>
   /// Adds a service of the type specified in <typeparamref name="TService" /> with an
   /// implementation type specified in <typeparamref name="TImplementation" /> to the
   /// <paramref name="services"/>.
   /// </summary>
   /// <param name="services">The <see cref="IServiceCollection" /> to add the service to.</param>
   /// <param name="lifetime">Lifetime of the service.</param>
   /// <typeparam name="TService">The type of the service to add.</typeparam>
   /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
   /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
   public static void Add<TService, TImplementation>(this IServiceCollection services, ServiceLifetime lifetime)
      where TImplementation : TService
   {
      ArgumentNullException.ThrowIfNull(services);

      services.Add(ServiceDescriptor.Describe(typeof(TService), typeof(TImplementation), lifetime));
   }
}
