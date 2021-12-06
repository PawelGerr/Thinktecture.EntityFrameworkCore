using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.Collections;

internal sealed class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
   IQueryProvider IQueryable.Provider => new AsyncQueryProvider(this);

   public AsyncEnumerable(IEnumerable<T> enumerable)
      : base(enumerable)
   {
      if (enumerable == null)
         throw new ArgumentNullException(nameof(enumerable));
   }

   private AsyncEnumerable(Expression expression)
      : base(expression)
   {
      if (expression == null)
         throw new ArgumentNullException(nameof(expression));
   }

   public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
   {
      return new AsyncEnumerator(this.AsEnumerable().GetEnumerator());
   }

   private sealed class AsyncEnumerator : IAsyncEnumerator<T>
   {
      private readonly IEnumerator<T> _enumerator;

      public T Current => _enumerator.Current;

      public AsyncEnumerator(IEnumerator<T> enumerator)
      {
         _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
      }

      public ValueTask<bool> MoveNextAsync()
      {
         return new(_enumerator.MoveNext());
      }

      public ValueTask DisposeAsync()
      {
         _enumerator.Dispose();

         return default;
      }
   }

   [SuppressMessage("ReSharper", "EF1001")]
   private sealed class AsyncQueryProvider : IAsyncQueryProvider
   {
      private static readonly MethodInfo _genericCreateQuery = typeof(AsyncQueryProvider).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                                                                         .First(m => m.Name == nameof(CreateQuery) && m.IsGenericMethod);

      private readonly IQueryProvider _queryProvider;

      internal AsyncQueryProvider(IQueryProvider queryProvider)
      {
         _queryProvider = queryProvider ?? throw new ArgumentNullException(nameof(queryProvider));
      }

      public IQueryable CreateQuery(Expression expression)
      {
         if (IsQueryableType(expression.Type))
         {
            var entityType = expression.Type.GetGenericArguments()[0];

            if (entityType != typeof(T))
            {
               return (IQueryable?)_genericCreateQuery.MakeGenericMethod(entityType).Invoke(this, new object[] { expression })
                      ?? throw new Exception($"Could not make a generic query using the method '{nameof(AsyncQueryProvider)}.{nameof(CreateQuery)}' and expression '{expression}'");
            }
         }

         return new AsyncEnumerable<T>(expression);
      }

      private static bool IsQueryableType(Type type)
      {
         if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
            return true;

         return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryable<>));
      }

      public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
      {
         return new AsyncEnumerable<TElement>(expression);
      }

      public object? Execute(Expression expression)
      {
         return _queryProvider.Execute(expression);
      }

      public TResult Execute<TResult>(Expression expression)
      {
         return _queryProvider.Execute<TResult>(expression);
      }

      public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
      {
         return Execute<TResult>(expression);
      }
   }
}
