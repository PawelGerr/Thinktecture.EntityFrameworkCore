using System;
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
      void RegisterDecorator<TService>(IServiceCollection services, Type genericDecoratorTypeDefinition);

      /// <summary>
      /// Gets the lifetimes of the last registration of the service with the type <typeparamref name="TService"/>.
      /// </summary>
      /// <param name="services">Service collection.</param>
      /// <typeparam name="TService">Service type.</typeparam>
      /// <returns>The lifetime of the service with the type <typeparamref name="TService"/>.</returns>
      ServiceLifetime GetLifetime<TService>(IServiceCollection services);
   }
}
