using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
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
      }

      /// <inheritdoc />
      public bool ApplyServices(IServiceCollection services)
      {
         if (ExpressionFragmentTranslatorPluginSupport)
            RegisterCompositeExpressionFragmentTranslator(services);

         if (_expressionFragmentTranslators.Count > 0)
            services.AddSingleton<IExpressionFragmentTranslatorPlugin>(new ExpressionFragmentTranslatorPlugin(_expressionFragmentTranslators));

         return false;
      }

      private static void RegisterCompositeExpressionFragmentTranslator([NotNull] IServiceCollection services)
      {
         var index = GetIndexOfExpressionFragmentTranslator(services);
         var newDescriptor = ServiceDescriptor.Singleton(typeof(IExpressionFragmentTranslator), typeof(CompositeExpressionFragmentTranslator));

         if (index < 0)
         {
            services.Add(newDescriptor);
         }
         else
         {
            services[index] = newDescriptor;
         }
      }

      private static int GetIndexOfExpressionFragmentTranslator([NotNull] IServiceCollection services)
      {
         for (var i = services.Count - 1; i >= 0; i--)
         {
            var service = services[i];

            if (service.ServiceType == typeof(IExpressionFragmentTranslator))
            {
               if (service.ImplementationType == null || service.ImplementationType != typeof(RelationalCompositeExpressionFragmentTranslator))
               {
                  throw new NotSupportedException($@"Other implementations of '{nameof(IExpressionFragmentTranslator)}' than '{nameof(RelationalCompositeExpressionFragmentTranslator)}' have been found in the DI.
There may be another library that is trying to register a custom implementation of '{nameof(IExpressionFragmentTranslator)}'. Stopping replacement of the the service to prevent unexpected behavior. Found implementation type: {service.ImplementationType?.GetType().DisplayName()}");
               }

               return i;
            }
         }

         return -1;
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
   }
}
