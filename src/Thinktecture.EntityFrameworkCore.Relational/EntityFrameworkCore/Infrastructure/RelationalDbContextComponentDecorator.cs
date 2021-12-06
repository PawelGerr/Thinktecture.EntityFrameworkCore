using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Decorates EF Core components.
/// </summary>
[SuppressMessage("ReSharper", "EF1001")]
public sealed class RelationalDbContextComponentDecorator : IRelationalDbContextComponentDecorator
{
   /// <inheritdoc />
   public void RegisterDecorator<TService>(
      IServiceCollection services,
      Type genericDecoratorTypeDefinition)
   {
      ArgumentNullException.ThrowIfNull(services);
      ArgumentNullException.ThrowIfNull(genericDecoratorTypeDefinition);

      var (implementationType, lifetime, index) = GetLatestRegistration<TService>(services);

      services.Add(ServiceDescriptor.Describe(implementationType, implementationType, lifetime)); // type to decorate

      var decoratorType = genericDecoratorTypeDefinition.MakeGenericType(implementationType);
      services[index] = ServiceDescriptor.Describe(typeof(TService), decoratorType, lifetime);
   }

   private static (Type implementationType, ServiceLifetime lifetime, int index) GetLatestRegistration<TService>(IServiceCollection services)
   {
      var serviceType = typeof(TService);

      for (var i = services.Count - 1; i >= 0; i--)
      {
         var service = services[i];

         if (service.ServiceType != serviceType)
            continue;

         if (service.ImplementationType == null)
            throw new NotSupportedException($@"The registration of the Entity Framework Core service '{serviceType.FullName}' found but the service is not registered 'by type'.");

         if (service.ImplementationType == serviceType)
            throw new NotSupportedException($@"The implementation type '{service.ImplementationType.ShortDisplayName()}' cannot be the same as the service type '{serviceType.ShortDisplayName()}'.");

         return (service.ImplementationType, service.Lifetime, i);
      }

      throw new InvalidOperationException($@"No registration of the Entity Framework Core service '{serviceType.FullName}' found. Please make sure the database provider is registered first (via 'UseSqlServer' or 'UseSqlite' etc).");
   }
}
