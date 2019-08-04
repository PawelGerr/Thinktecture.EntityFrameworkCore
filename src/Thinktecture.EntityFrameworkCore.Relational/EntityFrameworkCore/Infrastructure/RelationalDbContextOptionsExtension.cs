using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions for DbContextOptions.
   /// </summary>
   public class RelationalDbContextOptionsExtension : IDbContextOptionsExtension
   {
      private readonly List<ServiceDescriptor> _serviceDescriptors;
      private bool _activateExpressionFragmentTranslatorPluginSupport;
      private bool _addSchemaAwareComponents;

      /// <inheritdoc />
      [NotNull]
      public string LogFragment => $@"
{{
   'ExpressionFragmentTranslatorPluginSupport'={_activateExpressionFragmentTranslatorPluginSupport},
   'Number of custom services'={_serviceDescriptors.Count},
   'SchemaAwareComponents added'={_addSchemaAwareComponents},
   'DescendingSupport'={AddDescendingSupport}
}}";

      /// <summary>
      /// Indication whether to add support for "order by desc".
      /// </summary>
      public bool AddDescendingSupport { get; set; }

      /// <summary>
      /// Initializes new instance of <see cref="RelationalDbContextOptionsExtension"/>.
      /// </summary>
      public RelationalDbContextOptionsExtension()
      {
         _serviceDescriptors = new List<ServiceDescriptor>();
      }

      /// <inheritdoc />
      public bool ApplyServices(IServiceCollection services)
      {
         services.TryAddSingleton(this);
         services.AddSingleton<IMethodCallTranslatorPlugin, RelationalMethodCallTranslatorPlugin>();

         if (_activateExpressionFragmentTranslatorPluginSupport)
            RegisterCompositeExpressionFragmentTranslator(services);

         if (_addSchemaAwareComponents)
            RegisterSchemaAwareComponents(services);

         foreach (var descriptor in _serviceDescriptors)
         {
            services.Add(descriptor);
         }

         return false;
      }

      private static void RegisterSchemaAwareComponents([NotNull] IServiceCollection services)
      {
         services.AddSingleton<IMigrationOperationSchemaSetter, MigrationOperationSchemaSetter>();

         RegisterDecorator<IModelCacheKeyFactory>(services, typeof(DbSchemaAwareModelCacheKeyFactory<>));
         RegisterDecorator<IModelCustomizer>(services, typeof(DbSchemaAwareModelCustomizer<>));
         RegisterDecorator<IMigrationsAssembly>(services, typeof(DbSchemaAwareMigrationAssembly<>));
      }

      private static void RegisterCompositeExpressionFragmentTranslator([NotNull] IServiceCollection services)
      {
         RegisterDecorator<IExpressionFragmentTranslator>(services, typeof(CompositeExpressionFragmentTranslator<>));
      }

      private static void RegisterDecorator<TService>([NotNull] IServiceCollection services, [NotNull] Type genericDecoratorTypeDefinition)
      {
         if (genericDecoratorTypeDefinition == null)
            throw new ArgumentNullException(nameof(genericDecoratorTypeDefinition));

         var (implementationType, lifetime, index) = GetLatestRegistration<TService>(services);

         services.Add(ServiceDescriptor.Describe(implementationType, implementationType, lifetime)); // type to decorate

         var decoratorType = genericDecoratorTypeDefinition.MakeGenericType(implementationType);
         services[index] = ServiceDescriptor.Describe(typeof(TService), decoratorType, lifetime);
      }

      private static (Type implementationType, ServiceLifetime lifetime, int index) GetLatestRegistration<TService>([NotNull] IServiceCollection services)
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

         throw new NotSupportedException($@"No registration of the Entity Framework Core service '{typeof(TService).FullName}' found. Please make sure the database provider is registered (via 'UseSqlServer' or 'UseSqlite') before calling extensions methods like '{nameof(DbContextOptionsBuilderExtensions.AddExpressionFragmentTranslator)}'.");
      }

      /// <inheritdoc />
      public long GetServiceProviderHashCode()
      {
         return 0;
      }

      /// <inheritdoc />
      public void Validate(IDbContextOptions options)
      {
      }

      /// <summary>
      /// Adds a custom <see cref="IExpressionFragmentTranslatorPlugin"/> to dependency injection.
      /// </summary>
      /// <param name="type">Translator plugin to add.</param>
      /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
      public void AddExpressionFragmentTranslatorPlugin([NotNull] Type type)
      {
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         if (!typeof(IExpressionFragmentTranslatorPlugin).IsAssignableFrom(type))
            throw new ArgumentException($"The provided type '{type.DisplayName()}' must implement '{nameof(IExpressionFragmentTranslatorPlugin)}'.", nameof(type));

         Add(ServiceDescriptor.Singleton(typeof(IExpressionFragmentTranslatorPlugin), type));
         _activateExpressionFragmentTranslatorPluginSupport = true;
      }

      /// <summary>
      /// Adds provided <paramref name="type"/> to dependency injection.
      /// </summary>
      /// <param name="type">An implementation of <see cref="IRelationalTypeMappingSourcePlugin"/>.</param>
      /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
      public void AddRelationalTypeMappingSourcePlugin([NotNull] Type type)
      {
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         if (!typeof(IRelationalTypeMappingSourcePlugin).IsAssignableFrom(type))
            throw new ArgumentException($"The provided type '{type.DisplayName()}' must implement '{nameof(IRelationalTypeMappingSourcePlugin)}'.", nameof(type));

         Add(ServiceDescriptor.Singleton(typeof(IRelationalTypeMappingSourcePlugin), type));
      }

      /// <summary>
      /// Adds a service descriptor for registration of custom services with internal dependency injection container of Entity Framework Core.
      /// </summary>
      /// <param name="serviceDescriptor">Service descriptor to add.</param>
      /// <exception cref="ArgumentNullException"><paramref name="serviceDescriptor"/> is <c>null</c>.</exception>
      public void Add([NotNull] ServiceDescriptor serviceDescriptor)
      {
         if (serviceDescriptor == null)
            throw new ArgumentNullException(nameof(serviceDescriptor));

         _serviceDescriptors.Add(serviceDescriptor);
      }

      /// <summary>
      /// Adds components so Entity Framework Core can handle changes of the database schema at runtime.
      /// </summary>
      public void AddSchemaAwareComponents()
      {
         _addSchemaAwareComponents = true;
      }
   }
}
