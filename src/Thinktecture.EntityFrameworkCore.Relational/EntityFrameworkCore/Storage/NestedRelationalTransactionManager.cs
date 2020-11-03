using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Thinktecture.EntityFrameworkCore.Storage
{
   /// <summary>
   /// Transaction manager with nested transaction support.
   /// </summary>
   public class NestedRelationalTransactionManager : IRelationalTransactionManager
   {
      private readonly IDiagnosticsLogger<RelationalDbLoggerCategory.NestedTransaction> _logger;
      private readonly IRelationalTransactionManager _innerManager;
      private readonly Stack<NestedDbContextTransaction> _transactions;

      /// <inheritdoc />
      public IDbContextTransaction? CurrentTransaction => CurrentNestedTransaction;

      private NestedDbContextTransaction? CurrentNestedTransaction => _transactions.FirstOrDefault();

      /// <summary>
      /// Initializes new instance of <see cref="NestedRelationalTransactionManager"/>.
      /// </summary>
      /// <param name="logger">Logger.</param>
      /// <param name="innerManager">"Real" transaction manager, i.e. the one of the current database provider.</param>
      public NestedRelationalTransactionManager(
         IDiagnosticsLogger<RelationalDbLoggerCategory.NestedTransaction> logger,
         IRelationalTransactionManager innerManager)
      {
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
         _innerManager = innerManager ?? throw new ArgumentNullException(nameof(innerManager));
         _transactions = new Stack<NestedDbContextTransaction>();
      }

      /// <inheritdoc />
      public IDbContextTransaction? UseTransaction(
         DbTransaction? transaction)
      {
         if (transaction == null)
         {
            _logger.Logger.LogInformation($"Setting {nameof(DbTransaction)} to null.");
            _innerManager.UseTransaction(null);
            ClearTransactions();
         }
         else
         {
            _logger.Logger.LogInformation($"Setting {nameof(DbTransaction)} to the provided one.");
            var tx = _innerManager.UseTransaction(transaction);

            if (tx == null)
            {
               _logger.Logger.LogWarning("The inner transaction manager returned 'null' although the provided one is not null.");
               ClearTransactions();
            }
            else
            {
#pragma warning disable CA2000
               _transactions.Push(new RootNestedDbContextTransaction(_logger, this, _innerManager, tx));
#pragma warning restore CA2000
            }
         }

         return CurrentTransaction;
      }

      /// <inheritdoc />
      public async Task<IDbContextTransaction?> UseTransactionAsync(
         DbTransaction? transaction,
         CancellationToken cancellationToken = default)
      {
         if (transaction == null)
         {
            _logger.Logger.LogInformation($"Setting {nameof(DbTransaction)} to null.");
            await _innerManager.UseTransactionAsync(null, cancellationToken).ConfigureAwait(false);
            ClearTransactions();
         }
         else
         {
            _logger.Logger.LogInformation($"Setting {nameof(DbTransaction)} to the provided one.");
            var tx = await _innerManager.UseTransactionAsync(transaction, cancellationToken).ConfigureAwait(false);

            if (tx == null)
            {
               _logger.Logger.LogWarning("The inner transaction manager returned 'null' although the provided one is not null.");
               ClearTransactions();
            }
            else
            {
#pragma warning disable CA2000
               _transactions.Push(new RootNestedDbContextTransaction(_logger, this, _innerManager, tx));
#pragma warning restore CA2000
            }
         }

         return CurrentTransaction;
      }

      /// <inheritdoc />
      public void ResetState()
      {
         _logger.Logger.LogInformation("Resetting inner state.");
         _innerManager.ResetState();

         while (_transactions.Count > 0)
         {
            _transactions.Pop().Dispose();
         }
      }

      /// <inheritdoc />
      public async Task ResetStateAsync(CancellationToken cancellationToken = default)
      {
         _logger.Logger.LogInformation($"Resetting inner state.");
         await _innerManager.ResetStateAsync(cancellationToken).ConfigureAwait(false);

         while (_transactions.Count > 0)
         {
            await _transactions.Pop().DisposeAsync().ConfigureAwait(false);
         }
      }

      /// <inheritdoc />
      public IDbContextTransaction BeginTransaction()
      {
         return BeginTransactionInternal(null);
      }

      /// <inheritdoc />
      public IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel)
      {
         return BeginTransactionInternal(isolationLevel);
      }

      /// <inheritdoc />
      public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
      {
         return BeginTransactionInternalAsync(null, cancellationToken);
      }

      /// <inheritdoc />
      public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = new CancellationToken())
      {
         return BeginTransactionInternalAsync(isolationLevel, cancellationToken);
      }

      private IDbContextTransaction BeginTransactionInternal(IsolationLevel? isolationLevel)
      {
         var currentTx = CurrentNestedTransaction;

         if (currentTx != null)
         {
            currentTx = currentTx.BeginTransaction(isolationLevel);
            _logger.Logger.LogInformation("Started a child transaction with id '{TransactionId}' using isolation level '{IsolationLevel}'.", currentTx.TransactionId, isolationLevel);
         }
         else
         {
            var tx = isolationLevel.HasValue ? _innerManager.BeginTransaction(isolationLevel.Value) : _innerManager.BeginTransaction();
            currentTx = new RootNestedDbContextTransaction(_logger, this, _innerManager, tx);
            _logger.Logger.LogInformation("Started a root transaction with id '{TransactionId}' using isolation level '{IsolationLevel}'.", currentTx.TransactionId, isolationLevel);
         }

         _transactions.Push(currentTx);

         return currentTx;
      }

      private async Task<IDbContextTransaction> BeginTransactionInternalAsync(IsolationLevel? isolationLevel, CancellationToken cancellationToken)
      {
         var currentTx = CurrentNestedTransaction;

         if (currentTx != null)
         {
            currentTx = currentTx.BeginTransaction(isolationLevel);
            _logger.Logger.LogInformation("Started a child transaction with id '{TransactionId}' using isolation level '{IsolationLevel}'.", currentTx.TransactionId, isolationLevel);
         }
         else
         {
            var tx = await (isolationLevel.HasValue
                               ? _innerManager.BeginTransactionAsync(isolationLevel.Value, cancellationToken)
                               : _innerManager.BeginTransactionAsync(cancellationToken))
                        .ConfigureAwait(false);
            currentTx = new RootNestedDbContextTransaction(_logger, this, _innerManager, tx);
            _logger.Logger.LogInformation("Started a root transaction with id '{TransactionId}' using isolation level '{IsolationLevel}'.", currentTx.TransactionId, isolationLevel);
         }

         _transactions.Push(currentTx);

         return currentTx;
      }

      /// <inheritdoc />
      public void CommitTransaction()
      {
         if (_transactions.Count == 0)
            throw new InvalidOperationException("The connection does not have any active transactions.");

         _transactions.Pop().Commit();
      }

      /// <inheritdoc />
      public void RollbackTransaction()
      {
         if (_transactions.Count == 0)
            throw new InvalidOperationException("The connection does not have any active transactions.");

         _transactions.Pop().Rollback();
      }

      private void ClearTransactions()
      {
         _transactions.Clear();
      }

      internal void Remove(NestedDbContextTransaction transaction)
      {
         if (!_transactions.Contains(transaction))
            return;

         while (_transactions.Count > 0)
         {
            var tx = _transactions.Pop();

            if (tx == transaction)
               return;

            tx.Dispose();
         }
      }

      internal async ValueTask RemoveAsync(
         NestedDbContextTransaction transaction)
      {
         if (!_transactions.Contains(transaction))
            return;

         while (_transactions.Count > 0)
         {
            var tx = _transactions.Pop();

            if (tx == transaction)
               return;

            await tx.DisposeAsync().ConfigureAwait(false);
         }
      }
   }
}
