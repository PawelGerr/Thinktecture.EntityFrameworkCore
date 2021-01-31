using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;
using Thinktecture.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions for DbContextOptions.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class RelationalDbContextOptionsExtension : IDbContextOptionsExtension
   {
      private static readonly IRelationalDbContextComponentDecorator _defaultDecorator = new RelationalDbContextComponentDecorator();

      private readonly List<ServiceDescriptor> _serviceDescriptors;
      private readonly List<Type> _evaluatableExpressionFilterPlugins;

      private DbContextOptionsExtensionInfo? _info;

      /// <inheritdoc />
      public DbContextOptionsExtensionInfo Info => _info ??= new RelationalDbContextOptionsExtensionInfo(this);

      /// <summary>
      /// Adds components so Entity Framework Core can handle changes of the database schema at runtime.
      /// </summary>
      public bool AddSchemaRespectingComponents { get; set; }

      private IRelationalDbContextComponentDecorator? _componentDecorator;

      /// <summary>
      /// Decorates components.
      /// </summary>
      public IRelationalDbContextComponentDecorator ComponentDecorator
      {
         get => _componentDecorator ?? _defaultDecorator;
         set => _componentDecorator = value;
      }

      /// <summary>
      /// Adds support for nested transactions.
      /// </summary>
      public bool AddNestedTransactionsSupport { get; set; }

      /// <summary>
      /// Enables and disables support for "RowNumber".
      /// </summary>
      public bool AddRowNumberSupport { get; set; }

      /// <summary>
      /// Enables and disables support for 'tenant database support'.
      /// </summary>
      public bool AddTenantDatabaseSupport { get; set; }

      private bool _addCustomRelationalQueryContextFactory;

      /// <summary>
      /// A custom factory is registered if <c>true</c>.
      /// The factory is required for some features.
      /// </summary>
      public bool AddCustomRelationalQueryContextFactory
      {
         get => _addCustomRelationalQueryContextFactory || AddTenantDatabaseSupport;
         set => _addCustomRelationalQueryContextFactory = value;
      }

      /// <summary>
      /// Initializes new instance of <see cref="RelationalDbContextOptionsExtension"/>.
      /// </summary>
      public RelationalDbContextOptionsExtension()
      {
         _serviceDescriptors = new List<ServiceDescriptor>();
         _evaluatableExpressionFilterPlugins = new List<Type>();
      }

      /// <inheritdoc />
      public void ApplyServices(IServiceCollection services)
      {
         if (services == null)
            throw new ArgumentNullException(nameof(services));

         services.TryAddSingleton(this);
         services.TryAddSingleton<ITenantDatabaseProviderFactory>(DummyTenantDatabaseProviderFactory.Instance);

         services.Add<IMethodCallTranslatorPlugin, RelationalMethodCallTranslatorPlugin>(GetLifetime<IMethodCallTranslatorPlugin>());

         if (AddCustomRelationalQueryContextFactory)
            ComponentDecorator.RegisterDecorator<IQueryContextFactory>(services, typeof(ThinktectureRelationalQueryContextFactory<>));

         if (_evaluatableExpressionFilterPlugins.Count > 0)
         {
            var lifetime = GetLifetime<IEvaluatableExpressionFilterPlugin>();

            foreach (var plugin in _evaluatableExpressionFilterPlugins)
            {
               services.Add(ServiceDescriptor.Describe(typeof(IEvaluatableExpressionFilterPlugin), plugin, lifetime));
            }
         }

         if (AddSchemaRespectingComponents)
            RegisterDefaultSchemaRespectingComponents(services);

         if (AddNestedTransactionsSupport)
            services.Add(ServiceDescriptor.Describe(typeof(IDbContextTransactionManager), typeof(NestedRelationalTransactionManager), GetLifetime<IDbContextTransactionManager>()));

         foreach (var descriptor in _serviceDescriptors)
         {
            services.Add(descriptor);
         }
      }

      /// <summary>
      /// Gets the lifetime of a Entity Framework Core service.
      /// </summary>
      /// <typeparam name="TService">Service to fetch lifetime for.</typeparam>
      /// <returns>Lifetime of the provided service.</returns>
      /// <exception cref="InvalidOperationException">If service is not found.</exception>
      public static ServiceLifetime GetLifetime<TService>()
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
      public static void AddWithCheck<TService, TImplementation, TExpectedImplementation>(IServiceCollection services)
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

      private void RegisterDefaultSchemaRespectingComponents(IServiceCollection services)
      {
         services.TryAddSingleton<IMigrationOperationSchemaSetter, MigrationOperationSchemaSetter>();

         ComponentDecorator.RegisterDecorator<IModelCacheKeyFactory>(services, typeof(DefaultSchemaRespectingModelCacheKeyFactory<>));
         ComponentDecorator.RegisterDecorator<IModelCustomizer>(services, typeof(DefaultSchemaModelCustomizer<>));
         ComponentDecorator.RegisterDecorator<IMigrationsAssembly>(services, typeof(DefaultSchemaRespectingMigrationAssembly<>));
      }

      /// <inheritdoc />
      public void Validate(IDbContextOptions options)
      {
         if (AddTenantDatabaseSupport && _serviceDescriptors.All(d => d.ServiceType != typeof(ITenantDatabaseProviderFactory)))
            throw new InvalidOperationException($"TenantDatabaseSupport is enabled but there is no registration of an implementation of '{nameof(ITenantDatabaseProviderFactory)}'.");
      }

      /// <summary>
      /// Adds provided <paramref name="type"/> to dependency injection.
      /// </summary>
      /// <param name="type">An implementation of <see cref="IRelationalTypeMappingSourcePlugin"/>.</param>
      /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
      public void AddRelationalTypeMappingSourcePlugin(Type type)
      {
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         if (!typeof(IRelationalTypeMappingSourcePlugin).IsAssignableFrom(type))
            throw new ArgumentException($"The provided type '{type.ShortDisplayName()}' must implement '{nameof(IRelationalTypeMappingSourcePlugin)}'.", nameof(type));

         Add(ServiceDescriptor.Singleton(typeof(IRelationalTypeMappingSourcePlugin), type));
      }

      /// <summary>
      /// Adds a service descriptor for registration of custom services with internal dependency injection container of Entity Framework Core.
      /// </summary>
      /// <param name="serviceDescriptor">Service descriptor to add.</param>
      /// <exception cref="ArgumentNullException"><paramref name="serviceDescriptor"/> is <c>null</c>.</exception>
      public void Add(ServiceDescriptor serviceDescriptor)
      {
         if (serviceDescriptor == null)
            throw new ArgumentNullException(nameof(serviceDescriptor));

         _serviceDescriptors.Add(serviceDescriptor);
      }

      /// <summary>
      /// Adds an <see cref="IEvaluatableExpressionFilterPlugin"/> to the dependency injection.
      /// </summary>
      /// <typeparam name="T">Type of the plugin.</typeparam>
      public void AddEvaluatableExpressionFilterPlugin<T>()
         where T : IEvaluatableExpressionFilterPlugin
      {
         var type = typeof(T);

         if (!_evaluatableExpressionFilterPlugins.Contains(type))
            _evaluatableExpressionFilterPlugins.Add(type);
      }

      private class RelationalDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
      {
         private readonly RelationalDbContextOptionsExtension _extension;

         public override bool IsDatabaseProvider => false;

         private string? _logFragment;

         public override string LogFragment => _logFragment ??= $@"
{{
   'Custom RelationalQueryContextFactory'={_extension.AddCustomRelationalQueryContextFactory},
   'Default schema respecting components added'={_extension.AddSchemaRespectingComponents},
   'NestedTransactionsSupport'={_extension.AddNestedTransactionsSupport},
   'RowNumberSupport'={_extension.AddRowNumberSupport},
   'TenantDatabaseSupport'={_extension.AddTenantDatabaseSupport},
   'Number of evaluatable expression filter plugins'={_extension._evaluatableExpressionFilterPlugins.Count},
   'Number of custom services'={_extension._serviceDescriptors.Count}
}}";

         public RelationalDbContextOptionsExtensionInfo(RelationalDbContextOptionsExtension extension)
            : base(extension)
         {
            _extension = extension ?? throw new ArgumentNullException(nameof(extension));
         }

         public override long GetServiceProviderHashCode()
         {
            var hashCode = new HashCode();
            hashCode.Add(_extension.AddCustomRelationalQueryContextFactory);
            hashCode.Add(_extension.AddSchemaRespectingComponents);
            hashCode.Add(_extension.AddNestedTransactionsSupport);
            hashCode.Add(_extension.AddTenantDatabaseSupport);
            hashCode.Add(_extension.AddRowNumberSupport);
            hashCode.Add(_extension.ComponentDecorator);

            _extension._evaluatableExpressionFilterPlugins.ForEach(type => hashCode.Add(type));
            _extension._serviceDescriptors.ForEach(descriptor => hashCode.Add(GetHashCode(descriptor)));

            // Following switches doesn't add any new components:
            //   AddCustomRelationalParameterBasedSqlProcessorFactory

            return hashCode.ToHashCode();
         }

         private static int GetHashCode(ServiceDescriptor descriptor)
         {
            int implHashcode;

            if (descriptor.ImplementationType != null)
            {
               implHashcode = descriptor.ImplementationType.GetHashCode();
            }
            else if (descriptor.ImplementationInstance != null)
            {
               implHashcode = descriptor.ImplementationInstance.GetHashCode();
            }
            else
            {
               implHashcode = descriptor.ImplementationFactory?.GetHashCode()
                              ?? throw new ArgumentException("The service descriptor has no ImplementationType, ImplementationInstance and ImplementationFactory.");
            }

            return HashCode.Combine(descriptor.Lifetime, descriptor.ServiceType, implHashcode);
         }

         public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
         {
            debugInfo["Thinktecture:CustomRelationalQueryContextFactory"] = _extension.AddCustomRelationalQueryContextFactory.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:SchemaRespectingComponents"] = _extension.AddSchemaRespectingComponents.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:NestedTransactionsSupport"] = _extension.AddNestedTransactionsSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:RowNumberSupport"] = _extension.AddRowNumberSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:TenantDatabaseSupport"] = _extension.AddTenantDatabaseSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:EvaluatableExpressionFilterPlugins"] = String.Join(", ", _extension._evaluatableExpressionFilterPlugins.Select(t => t.ShortDisplayName()));
            debugInfo["Thinktecture:ServiceDescriptors"] = String.Join(", ", _extension._serviceDescriptors);
         }
      }
   }
}
