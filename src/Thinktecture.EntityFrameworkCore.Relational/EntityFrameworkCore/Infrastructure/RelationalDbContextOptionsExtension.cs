using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Extensions for DbContextOptions.
/// </summary>
[SuppressMessage("ReSharper", "EF1001")]
public sealed class RelationalDbContextOptionsExtension : DbContextOptionsExtensionBase, IDbContextOptionsExtension
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
      ArgumentNullException.ThrowIfNull(services);

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
      ArgumentNullException.ThrowIfNull(type);

      if (!typeof(IRelationalTypeMappingSourcePlugin).IsAssignableFrom(type))
         throw new ArgumentException($"The provided type '{type.ShortDisplayName()}' must implement '{nameof(IRelationalTypeMappingSourcePlugin)}'.", nameof(type));

      Register(typeof(IRelationalTypeMappingSourcePlugin), type, ServiceLifetime.Singleton);
   }

   /// <summary>
   /// Registers a custom service with internal dependency injection container of Entity Framework Core.
   /// </summary>
   /// <param name="serviceType">Service type.</param>
   /// <param name="implementationType">Implementation type.</param>
   /// <param name="lifetime">Service lifetime.</param>
   /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> or <paramref name="implementationType"/> is <c>null</c>.</exception>
   public void Register(Type serviceType, Type implementationType, ServiceLifetime lifetime)
   {
      ArgumentNullException.ThrowIfNull(serviceType);

      ArgumentNullException.ThrowIfNull(implementationType);

      _serviceDescriptors.Add(ServiceDescriptor.Describe(serviceType, implementationType, lifetime));
   }

   /// <summary>
   /// Registers a custom service instance with internal dependency injection container of Entity Framework Core.
   /// </summary>
   /// <param name="serviceType">Service type.</param>
   /// <param name="implementationInstance">Implementation instance.</param>
   /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> or <paramref name="implementationInstance"/> is <c>null</c>.</exception>
   public void Register(Type serviceType, object implementationInstance)
   {
      ArgumentNullException.ThrowIfNull(serviceType);

      ArgumentNullException.ThrowIfNull(implementationInstance);

      _serviceDescriptors.Add(ServiceDescriptor.Singleton(serviceType, implementationInstance));
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

      public override int GetServiceProviderHashCode()
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

      /// <inheritdoc />
      public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
      {
         if (other is not RelationalDbContextOptionsExtensionInfo otherRelationalInfo)
            return false;

         var areEqual = _extension.AddCustomRelationalQueryContextFactory == otherRelationalInfo._extension.AddCustomRelationalQueryContextFactory
                        && _extension.AddSchemaRespectingComponents == otherRelationalInfo._extension.AddSchemaRespectingComponents
                        && _extension.AddNestedTransactionsSupport == otherRelationalInfo._extension.AddNestedTransactionsSupport
                        && _extension.AddTenantDatabaseSupport == otherRelationalInfo._extension.AddTenantDatabaseSupport
                        && _extension.AddRowNumberSupport == otherRelationalInfo._extension.AddRowNumberSupport
                        && _extension.ComponentDecorator.Equals(otherRelationalInfo._extension.ComponentDecorator);

         if (!areEqual)
            return false;

         if (_extension._evaluatableExpressionFilterPlugins.Count != otherRelationalInfo._extension._evaluatableExpressionFilterPlugins.Count)
            return false;

         if (_extension._evaluatableExpressionFilterPlugins.Except(otherRelationalInfo._extension._evaluatableExpressionFilterPlugins).Any())
            return false;

         if (_extension._serviceDescriptors.Count != otherRelationalInfo._extension._serviceDescriptors.Count)
            return false;

         return _extension._serviceDescriptors.All(d => otherRelationalInfo._extension._serviceDescriptors.Any(o => AreEqual(d, o)));
      }

      private static bool AreEqual(ServiceDescriptor serviceDescriptor, ServiceDescriptor other)
      {
         if (serviceDescriptor.Lifetime != other.Lifetime || serviceDescriptor.ServiceType != other.ServiceType)
            return false;

         if (serviceDescriptor.ImplementationType is not null)
            return serviceDescriptor.ImplementationType == other.ImplementationType;

         if (serviceDescriptor.ImplementationInstance is not null)
            return serviceDescriptor.ImplementationInstance.Equals(other.ImplementationInstance);

         throw new NotSupportedException("Implementation factories are not supported.");
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
            throw new NotSupportedException("Implementation factories are not supported.");
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
