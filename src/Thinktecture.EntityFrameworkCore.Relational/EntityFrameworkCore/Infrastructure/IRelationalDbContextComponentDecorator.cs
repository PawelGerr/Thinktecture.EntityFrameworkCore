using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Decorates EF Core components.
   /// </summary>
   public interface IRelationalDbContextComponentDecorator
   {
      /// <summary>
      /// Decorates the service of type <typeparamref name="TService"/> with a decorator of type <paramref name="genericDecoratorTypeDefinition"/>.
      /// </summary>
      /// <param name="services">Service collection.</param>
      /// <param name="genericDecoratorTypeDefinition">Generic type definition.</param>
      /// <typeparam name="TService">Service type.</typeparam>
      void RegisterDecorator<TService>([NotNull] IServiceCollection services, [NotNull] Type genericDecoratorTypeDefinition);
   }
}
