using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Decorates EF Core components.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public class RelationalDbContextComponentDecorator : IRelationalDbContextComponentDecorator
   {
      /// <inheritdoc />
      public void RegisterDecorator<TService>(IServiceCollection services, Type genericDecoratorTypeDefinition)
      {
         if (genericDecoratorTypeDefinition == null)
            throw new ArgumentNullException(nameof(genericDecoratorTypeDefinition));

         var (implementationType, lifetime, index) = GetLatestRegistration<TService>(services);

         services.Add(ServiceDescriptor.Describe(implementationType, implementationType, lifetime)); // type to decorate

         var decoratorType = genericDecoratorTypeDefinition.MakeGenericType(implementationType);
         services[index] = ServiceDescriptor.Describe(typeof(TService), decoratorType, lifetime);
      }

      private static (Type implementationType, ServiceLifetime lifetime, int index) GetLatestRegistration<TService>(IServiceCollection services)
      {
         for (var i = services.Count - 1; i >= 0; i--)
         {
            var service = services[i];

            if (service.ServiceType == typeof(TService))
            {
               if (service.ImplementationType == null)
                  throw new NotSupportedException($@"The registration of the Entity Framework Core service '{typeof(TService).FullName}' found but the service is not registered 'by type'.");

               if (service.ImplementationType == typeof(TService))
                  throw new NotSupportedException($@"The implementation type '{service.ImplementationType.DisplayName()}' cannot be the same as the service type '{typeof(TService).DisplayName()}'.");

               return (service.ImplementationType, service.Lifetime, i);
            }
         }

         throw new NotSupportedException($@"No registration of the Entity Framework Core service '{typeof(TService).FullName}' found. Please make sure the database provider is registered (via 'UseSqlServer' or 'UseSqlite').");
      }
   }
}
