using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Thinktecture.Collections
{
   internal class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
   {
      IQueryProvider IQueryable.Provider => new AsyncQueryProvider<T>(this);

      public AsyncEnumerable([NotNull] IEnumerable<T> enumerable)
         : base(enumerable)
      {
         if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable));
      }

      public AsyncEnumerable([NotNull] Expression expression)
         : base(expression)
      {
         if (expression == null)
            throw new ArgumentNullException(nameof(expression));
      }

      [NotNull]
      public IAsyncEnumerator<T> GetEnumerator()
      {
         return new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
      }

      private class AsyncEnumerator<T> : IAsyncEnumerator<T>
      {
         private readonly IEnumerator<T> _enumerator;

         public T Current => _enumerator.Current;

         public AsyncEnumerator([NotNull] IEnumerator<T> enumerator)
         {
            _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
         }

         // ReSharper disable once TaskOfTMethodsWithoutAsyncSuffix
         [NotNull]
         public Task<bool> MoveNext(CancellationToken cancellationToken)
         {
            return Task.FromResult(_enumerator.MoveNext());
         }

         public void Dispose()
         {
            _enumerator.Dispose();
         }
      }

      private class AsyncQueryProvider<TEntity> : IAsyncQueryProvider
      {
         private readonly IQueryProvider _queryProvider;

         internal AsyncQueryProvider([NotNull] IQueryProvider queryProvider)
         {
            _queryProvider = queryProvider ?? throw new ArgumentNullException(nameof(queryProvider));
         }

         [NotNull]
         public IQueryable CreateQuery(Expression expression)
         {
            return new AsyncEnumerable<TEntity>(expression);
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

         [NotNull]
         public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
         {
            return new AsyncEnumerable<TResult>(expression);
         }

         [NotNull]
         public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
         {
            return Task.FromResult(Execute<TResult>(expression));
         }
      }
   }
}
