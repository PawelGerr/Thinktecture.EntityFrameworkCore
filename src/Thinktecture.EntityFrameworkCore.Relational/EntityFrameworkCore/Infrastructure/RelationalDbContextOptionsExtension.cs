using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions for DbContextOptions.
   /// </summary>
   public class RelationalDbContextOptionsExtension : IDbContextOptionsExtension
   {
      private readonly List<IExpressionFragmentTranslator> _expressionFragmentTranslators;
      private readonly List<Type> _typeMappingSourcePluginsTypes;

      /// <inheritdoc />
      [NotNull]
      public string LogFragment => $@"{{ 'ExpressionFragmentTranslatorPluginSupport'={ExpressionFragmentTranslatorPluginSupport}, 
'Custom ExpressionFragmentTranslators'=[{String.Join(",", _expressionFragmentTranslators.Select(t => t.GetType().DisplayName()))}] }}";

      private bool _expressionFragmentTranslatorPluginSupport;

      /// <summary>
      /// Enables and disables support for <see cref="IExpressionFragmentTranslatorPlugin"/>.
      /// </summary>
      public bool ExpressionFragmentTranslatorPluginSupport
      {
         get => _expressionFragmentTranslatorPluginSupport || _expressionFragmentTranslators.Count > 0;
         set => _expressionFragmentTranslatorPluginSupport = value;
      }

      /// <summary>
      /// Initializes new instance of <see cref="RelationalDbContextOptionsExtension"/>.
      /// </summary>
      public RelationalDbContextOptionsExtension()
      {
         _expressionFragmentTranslators = new List<IExpressionFragmentTranslator>();
         _typeMappingSourcePluginsTypes = new List<Type>();
      }

      /// <inheritdoc />
      public bool ApplyServices(IServiceCollection services)
      {
         if (ExpressionFragmentTranslatorPluginSupport)
            RegisterCompositeExpressionFragmentTranslator(services);

         if (_expressionFragmentTranslators.Count > 0)
            services.AddSingleton<IExpressionFragmentTranslatorPlugin>(new ExpressionFragmentTranslatorPlugin(_expressionFragmentTranslators));

         foreach (var sourcePluginsType in _typeMappingSourcePluginsTypes)
         {
            services.AddSingleton(typeof(IRelationalTypeMappingSourcePlugin), sourcePluginsType);
         }

         return false;
      }

      private static void RegisterCompositeExpressionFragmentTranslator([NotNull] IServiceCollection services)
      {
         var (implementationType, index) = GetLatestRegistration<IExpressionFragmentTranslator>(services);

         services.AddSingleton(implementationType); // type to decorate

         var decoratorType = typeof(CompositeExpressionFragmentTranslator<>).MakeGenericType(implementationType);
         var decoratorDescriptor = ServiceDescriptor.Singleton(typeof(IExpressionFragmentTranslator), decoratorType);

         services[index] = decoratorDescriptor;
      }

      private static (Type implementationType, int index) GetLatestRegistration<TService>([NotNull] IServiceCollection services)
      {
         for (var i = services.Count - 1; i >= 0; i--)
         {
            var service = services[i];

            if (service.ServiceType == typeof(TService))
            {
               if (service.ImplementationType == null)
                  throw new NotSupportedException($@"The registration of the Entity Framework Core service '{typeof(TService).FullName}' found but the service is not registered 'by type'.");

               return (service.ImplementationType, i);
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
      /// Adds a custom <see cref="IExpressionFragmentTranslator"/>.
      /// </summary>
      /// <param name="translator">Translator to add.</param>
      /// <exception cref="ArgumentNullException"><paramref name="translator"/> is <c>null</c>.</exception>
      public void AddExpressionFragmentTranslator([NotNull] IExpressionFragmentTranslator translator)
      {
         if (translator == null)
            throw new ArgumentNullException(nameof(translator));

         _expressionFragmentTranslators.Add(translator);
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

         _typeMappingSourcePluginsTypes.Add(type);
      }
   }
}
