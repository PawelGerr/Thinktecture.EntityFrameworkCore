using System.Data.Common;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using IsolationLevel = System.Data.IsolationLevel;

namespace Thinktecture.EntityFrameworkCore.Storage;

/// <summary>
/// Transaction manager with nested transaction support.
/// </summary>
public class NestedRelationalTransactionManager : IRelationalTransactionManager, ITransactionEnlistmentManager
{
   private readonly IDiagnosticsLogger<RelationalDbLoggerCategory.NestedTransaction> _logger;
   private readonly IRelationalConnection _innerManager;
   private readonly ITransactionEnlistmentManager? _enlistmentManager;
   private readonly Stack<NestedDbContextTransaction> _transactions;

   /// <inheritdoc />
   public IDbContextTransaction? CurrentTransaction => CurrentNestedTransaction;

   private NestedDbContextTransaction? CurrentNestedTransaction => _transactions.FirstOrDefault();

   /// <inheritdoc />
   public Transaction? EnlistedTransaction => (_enlistmentManager ?? throw new NotSupportedException("The current provider doesn't support System.Transaction.")).EnlistedTransaction;

   /// <summary>
   /// Initializes new instance of <see cref="NestedRelationalTransactionManager"/>.
   /// </summary>
   /// <param name="logger">Logger.</param>
   /// <param name="connection">Current connection which is the "real" transaction manager, i.e. the one of the current database provider.</param>
   public NestedRelationalTransactionManager(
      IDiagnosticsLogger<RelationalDbLoggerCategory.NestedTransaction> logger,
      IRelationalConnection connection)
   {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _innerManager = connection ?? throw new ArgumentNullException(nameof(connection));
      _enlistmentManager = connection as ITransactionEnlistmentManager;
      _transactions = new Stack<NestedDbContextTransaction>();
   }

   /// <inheritdoc />
   public void EnlistTransaction(Transaction? transaction)
   {
      (_enlistmentManager ?? throw new NotSupportedException("The current provider doesn't support System.Transaction."))
         .EnlistTransaction(transaction);
   }

   /// <inheritdoc />
   public IDbContextTransaction? UseTransaction(
      DbTransaction? transaction)
   {
      return UseTransactionInternal(transaction, null);
   }

   /// <inheritdoc />
   public IDbContextTransaction? UseTransaction(DbTransaction? transaction, Guid transactionId)
   {
      return UseTransactionInternal(transaction, transactionId);
   }

   private IDbContextTransaction? UseTransactionInternal(
      DbTransaction? transaction,
      Guid? transactionId)
   {
      if (transaction == null)
      {
         _logger.Logger.LogInformation($"Setting {nameof(DbTransaction)} to null.");

         if (transactionId.HasValue)
         {
            _innerManager.UseTransaction(null, transactionId.Value);
         }
         else
         {
            _innerManager.UseTransaction(null);
         }

         ClearTransactions();
      }
      else
      {
         _logger.Logger.LogInformation($"Setting {nameof(DbTransaction)} to the provided one.");
         var tx = transactionId.HasValue
                     ? _innerManager.UseTransaction(transaction, transactionId.Value)
                     : _innerManager.UseTransaction(transaction);

         if (tx == null)
         {
            _logger.Logger.LogWarning("The inner transaction manager returned 'null' although the provided one is not null.");
            ClearTransactions();
         }
         else
         {
            _transactions.Push(new RootNestedDbContextTransaction(_logger, this, _innerManager, tx, transactionId));
         }
      }

      return CurrentTransaction;
   }

   /// <inheritdoc />
   public Task<IDbContextTransaction?> UseTransactionAsync(
      DbTransaction? transaction,
      CancellationToken cancellationToken = default)
   {
      return UseTransactionInternalAsync(transaction, null, cancellationToken);
   }

   /// <inheritdoc />
   public Task<IDbContextTransaction?> UseTransactionAsync(
      DbTransaction? transaction,
      Guid transactionId,
      CancellationToken cancellationToken = default)
   {
      return UseTransactionInternalAsync(transaction, transactionId, cancellationToken);
   }

   private async Task<IDbContextTransaction?> UseTransactionInternalAsync(
      DbTransaction? transaction,
      Guid? transactionId,
      CancellationToken cancellationToken)
   {
      if (transaction == null)
      {
         _logger.Logger.LogInformation($"Setting {nameof(DbTransaction)} to null.");

         if (transactionId.HasValue)
         {
            await _innerManager.UseTransactionAsync(null, transactionId.Value, cancellationToken).ConfigureAwait(false);
         }
         else
         {
            await _innerManager.UseTransactionAsync(null, cancellationToken).ConfigureAwait(false);
         }

         ClearTransactions();
      }
      else
      {
         _logger.Logger.LogInformation($"Setting {nameof(DbTransaction)} to the provided one.");
         var tx = transactionId.HasValue
                     ? await _innerManager.UseTransactionAsync(transaction, transactionId.Value, cancellationToken).ConfigureAwait(false)
                     : await _innerManager.UseTransactionAsync(transaction, cancellationToken).ConfigureAwait(false);

         if (tx == null)
         {
            _logger.Logger.LogWarning("The inner transaction manager returned 'null' although the provided one is not null.");
            ClearTransactions();
         }
         else
         {
            _transactions.Push(new RootNestedDbContextTransaction(_logger, this, _innerManager, tx, transactionId));
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
      _logger.Logger.LogInformation("Resetting inner state.");
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
   public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
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
         currentTx = new RootNestedDbContextTransaction(_logger, this, _innerManager, tx, null);
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
         currentTx = new RootNestedDbContextTransaction(_logger, this, _innerManager, tx, null);
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
   public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
   {
      if (_transactions.Count == 0)
         throw new InvalidOperationException("The connection does not have any active transactions.");

      return _transactions.Pop().CommitAsync(cancellationToken);
   }

   /// <inheritdoc />
   public void RollbackTransaction()
   {
      if (_transactions.Count == 0)
         throw new InvalidOperationException("The connection does not have any active transactions.");

      _transactions.Pop().Rollback();
   }

   /// <inheritdoc />
   public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
   {
      if (_transactions.Count == 0)
         throw new InvalidOperationException("The connection does not have any active transactions.");

      return _transactions.Pop().RollbackAsync(cancellationToken);
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
