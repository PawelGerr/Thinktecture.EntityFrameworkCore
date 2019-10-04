using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
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
   [SuppressMessage("ReSharper", "EF1001")]
   public class RelationalDbContextOptionsExtension : IDbContextOptionsExtension
   {
      private static readonly IRelationalDbContextComponentDecorator _defaultDecorator = new RelationalDbContextComponentDecorator();

      private readonly List<ServiceDescriptor> _serviceDescriptors;

      private DbContextOptionsExtensionInfo _info;

      /// <inheritdoc />
      [JetBrains.Annotations.NotNull]
      public DbContextOptionsExtensionInfo Info => _info ??= new RelationalDbContextOptionsExtensionInfo(this);

      /// <summary>
      /// Indication whether to add support for "order by desc".
      /// </summary>
      public bool AddDescendingSupport { get; set; }

      /// <summary>
      /// Adds components so Entity Framework Core can handle changes of the database schema at runtime.
      /// </summary>
      public bool AddSchemaRespectingComponents { get; set; }

      /// <summary>
      /// Decorates components.
      /// </summary>
      public IRelationalDbContextComponentDecorator ComponentDecorator { get; set; }

      /// <summary>
      /// Initializes new instance of <see cref="RelationalDbContextOptionsExtension"/>.
      /// </summary>
      public RelationalDbContextOptionsExtension()
      {
         _serviceDescriptors = new List<ServiceDescriptor>();
      }

      /// <inheritdoc />
      public void ApplyServices(IServiceCollection services)
      {
         services.TryAddSingleton(this);
         services.Add<IMethodCallTranslatorPlugin, RelationalMethodCallTranslatorPlugin>(GetLifetime<IMethodCallTranslatorPlugin>());

         if (AddSchemaRespectingComponents)
            RegisterDefaultSchemaRespectingComponents(services);

         foreach (var descriptor in _serviceDescriptors)
         {
            services.Add(descriptor);
         }
      }

      private static ServiceLifetime GetLifetime<TService>()
      {
         return EntityFrameworkRelationalServicesBuilder.RelationalServices[typeof(TService)].Lifetime;
      }

      private void RegisterDefaultSchemaRespectingComponents([JetBrains.Annotations.NotNull] IServiceCollection services)
      {
         services.AddSingleton<IMigrationOperationSchemaSetter, MigrationOperationSchemaSetter>();
         var decorator = ComponentDecorator ?? _defaultDecorator;

         decorator.RegisterDecorator<IModelCacheKeyFactory>(services, typeof(DefaultSchemaRespectingModelCacheKeyFactory<>));
         decorator.RegisterDecorator<IModelCustomizer>(services, typeof(DefaultSchemaModelCustomizer<>));
         decorator.RegisterDecorator<IMigrationsAssembly>(services, typeof(DefaultSchemaRespectingMigrationAssembly<>));
      }

      /// <inheritdoc />
      public void Validate(IDbContextOptions options)
      {
      }

      /// <summary>
      /// Adds provided <paramref name="type"/> to dependency injection.
      /// </summary>
      /// <param name="type">An implementation of <see cref="IRelationalTypeMappingSourcePlugin"/>.</param>
      /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
      public void AddRelationalTypeMappingSourcePlugin([JetBrains.Annotations.NotNull] Type type)
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
      public void Add([JetBrains.Annotations.NotNull] ServiceDescriptor serviceDescriptor)
      {
         if (serviceDescriptor == null)
            throw new ArgumentNullException(nameof(serviceDescriptor));

         _serviceDescriptors.Add(serviceDescriptor);
      }

      private class RelationalDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
      {
         private readonly RelationalDbContextOptionsExtension _extension;

         public override bool IsDatabaseProvider => false;

         private string _logFragment;

         [JetBrains.Annotations.NotNull]
         public override string LogFragment => _logFragment ??= $@"
{{
   'Number of custom services'={_extension._serviceDescriptors.Count},
   'Default schema respecting components added'={_extension.AddSchemaRespectingComponents},
   'DescendingSupport'={_extension.AddDescendingSupport}
}}";

         public RelationalDbContextOptionsExtensionInfo([JetBrains.Annotations.NotNull] RelationalDbContextOptionsExtension extension)
            : base(extension)
         {
            _extension = extension ?? throw new ArgumentNullException(nameof(extension));
         }

         public override long GetServiceProviderHashCode()
         {
            return 0;
         }

         public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
         {
            debugInfo["Thinktecture:AddSchemaRespectingComponents"] = _extension.AddSchemaRespectingComponents.ToString(CultureInfo.InvariantCulture);
         }
      }
   }
}
