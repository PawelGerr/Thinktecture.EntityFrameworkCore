using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Thinktecture.Collections
{
   internal class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
   {
      IQueryProvider IQueryable.Provider => new AsyncQueryProvider(this);

      public AsyncEnumerable(IEnumerable<T> enumerable)
         : base(enumerable)
      {
         if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable));
      }

      public AsyncEnumerable(Expression expression)
         : base(expression)
      {
         if (expression == null)
            throw new ArgumentNullException(nameof(expression));
      }

      public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
      {
         return new AsyncEnumerator(this.AsEnumerable().GetEnumerator());
      }

      private class AsyncEnumerator : IAsyncEnumerator<T>
      {
         private readonly IEnumerator<T> _enumerator;

         public T Current => _enumerator.Current;

         public AsyncEnumerator(IEnumerator<T> enumerator)
         {
            _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
         }

         public ValueTask<bool> MoveNextAsync()
         {
            return new ValueTask<bool>(_enumerator.MoveNext());
         }

         public ValueTask DisposeAsync()
         {
            _enumerator.Dispose();

            return default;
         }
      }

      private class AsyncQueryProvider : IAsyncQueryProvider
      {
         private readonly IQueryProvider _queryProvider;

         internal AsyncQueryProvider(IQueryProvider queryProvider)
         {
            _queryProvider = queryProvider ?? throw new ArgumentNullException(nameof(queryProvider));
         }

         public IQueryable CreateQuery(Expression expression)
         {
            return new AsyncEnumerable<T>(expression);
         }

         public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
         {
            return new AsyncEnumerable<TElement>(expression);
         }

         public object Execute(Expression expression)
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
}
