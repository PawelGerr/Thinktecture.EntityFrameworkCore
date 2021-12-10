using System.Data.Common;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using IsolationLevel = System.Data.IsolationLevel;

namespace Thinktecture.EntityFrameworkCore.Storage;

/// <summary>
/// A nested transaction.
/// </summary>
public abstract class NestedDbContextTransaction : IDbContextTransaction, IInfrastructure<DbTransaction>
{
   private bool _isCommitted;
   private bool _isRolledBack;
   private bool _isDisposed;

   private Stack<NestedDbContextTransaction>? _children;

   /// <summary>
   /// Friendly transaction type name.
   /// </summary>
   protected abstract string TransactionTypeName { get; }

   /// <summary>
   /// Gets the indication whether the transaction is completed, i.e. committed/rolled back, or not.
   /// </summary>
   protected bool IsCompleted => _isCommitted || _isRolledBack;

   /// <summary>
   /// Nested transaction manager.
   /// </summary>
   protected NestedRelationalTransactionManager NestedTransactionManager { get; private set; }

   /// <summary>
   /// Logger.
   /// </summary>
   protected IDiagnosticsLogger<RelationalDbLoggerCategory.NestedTransaction> DiagnosticsLogger { get; }

   /// <inheritdoc />
   public Guid TransactionId { get; }

   /// <inheritdoc />
   DbTransaction IInfrastructure<DbTransaction>.Instance => GetUnderlyingTransaction();

   /// <summary>
   /// Gets the underlying <see cref="DbTransaction"/>.
   /// </summary>
   /// <returns>The underlying <see cref="DbTransaction"/>.</returns>
   protected internal abstract DbTransaction GetUnderlyingTransaction();

   /// <summary>
   /// Initializes new instance of <see cref="NestedDbContextTransaction"/>.
   /// </summary>
   /// <param name="logger">Logger.</param>
   /// <param name="nestedTransactionManager">Nested transaction manager.</param>
   /// <param name="transactionId">The transaction id.</param>
   protected NestedDbContextTransaction(
      IDiagnosticsLogger<RelationalDbLoggerCategory.NestedTransaction> logger,
      NestedRelationalTransactionManager nestedTransactionManager,
      Guid transactionId)
   {
      DiagnosticsLogger = logger ?? throw new ArgumentNullException(nameof(logger));
      NestedTransactionManager = nestedTransactionManager ?? throw new ArgumentNullException(nameof(nestedTransactionManager));
      TransactionId = transactionId;
   }

   /// <summary>
   /// Start a new child transaction.
   /// </summary>
   /// <returns>A child transaction.</returns>
   internal ChildNestedDbContextTransaction BeginTransaction(IsolationLevel? isolationLevel)
   {
      EnsureUsable();

      if (isolationLevel.HasValue)
         EnsureIsolationLevelCompatible(isolationLevel.Value);

      var child = new ChildNestedDbContextTransaction(DiagnosticsLogger, NestedTransactionManager, this);

      if (_children == null)
         _children = new Stack<NestedDbContextTransaction>();

      _children.Push(child);

      return child;
   }

   private void EnsureIsolationLevelCompatible(IsolationLevel isolationLevel)
   {
      var underlyingTransaction = GetUnderlyingTransaction();
      var currentIsolationLevel = underlyingTransaction.IsolationLevel;

      switch (isolationLevel)
      {
         case IsolationLevel.Unspecified:
            throw new ArgumentException($"The isolation level '{IsolationLevel.Unspecified}' is not allowed.", nameof(isolationLevel));

         case IsolationLevel.Chaos:
            if (currentIsolationLevel != IsolationLevel.Chaos)
               throw CreateException(isolationLevel, currentIsolationLevel);

            break;

         case IsolationLevel.ReadUncommitted:
            if (currentIsolationLevel != IsolationLevel.Serializable &&
                currentIsolationLevel != IsolationLevel.RepeatableRead &&
                currentIsolationLevel != IsolationLevel.ReadCommitted &&
                currentIsolationLevel != IsolationLevel.ReadUncommitted)
               throw CreateException(isolationLevel, currentIsolationLevel);

            break;

         case IsolationLevel.ReadCommitted:
            if (currentIsolationLevel != IsolationLevel.Serializable &&
                currentIsolationLevel != IsolationLevel.RepeatableRead &&
                currentIsolationLevel != IsolationLevel.ReadCommitted)
               throw CreateException(isolationLevel, currentIsolationLevel);

            break;

         case IsolationLevel.RepeatableRead:
            if (currentIsolationLevel != IsolationLevel.Serializable &&
                currentIsolationLevel != IsolationLevel.RepeatableRead)
               throw CreateException(isolationLevel, currentIsolationLevel);

            break;

         case IsolationLevel.Serializable:
            if (currentIsolationLevel != IsolationLevel.Serializable)
               throw CreateException(isolationLevel, currentIsolationLevel);

            break;

         case IsolationLevel.Snapshot:
            if (currentIsolationLevel != IsolationLevel.Snapshot)
               throw CreateException(isolationLevel, currentIsolationLevel);

            break;

         default:
            throw new ArgumentOutOfRangeException(nameof(isolationLevel), isolationLevel, $"Unknown {nameof(IsolationLevel)}.");
      }
   }

   private static InvalidOperationException CreateException(
      IsolationLevel newIsolationLevel,
      IsolationLevel currentIsolationLevel)
   {
      return new($"The isolation level '{currentIsolationLevel}' of the parent transaction is not compatible to the provided isolation level '{newIsolationLevel}'.");
   }

   /// <inheritdoc />
   public virtual void Commit()
   {
      EnsureUsable();
      EnsureChildrenCompleted();

      if (_children?.Any(c => !c._isCommitted) == true)
         throw new TransactionAbortedException("The transaction has aborted.");

      _isCommitted = true;
      NestedTransactionManager.Remove(this);

      DiagnosticsLogger.Logger.LogInformation("The {TransactionType} with id '{TransactionId}' is committed.", TransactionTypeName, TransactionId);
   }

   /// <inheritdoc />
   public virtual async Task CommitAsync(CancellationToken cancellationToken = default)
   {
      EnsureUsable();
      EnsureChildrenCompleted();

      if (_children?.Any(c => !c._isCommitted) == true)
         throw new TransactionAbortedException("The transaction has aborted.");

      _isCommitted = true;
      await NestedTransactionManager.RemoveAsync(this).ConfigureAwait(false);

      DiagnosticsLogger.Logger.LogInformation("The {TransactionType} with id '{TransactionId}' is committed.", TransactionTypeName, TransactionId);
   }

   /// <inheritdoc />
   public virtual void Rollback()
   {
      RollbackInternal();
      NestedTransactionManager.Remove(this);
   }

   /// <inheritdoc />
   public virtual async Task RollbackAsync(CancellationToken cancellationToken = default)
   {
      RollbackInternal();
      await NestedTransactionManager.RemoveAsync(this).ConfigureAwait(false);
   }

   private void RollbackInternal()
   {
      EnsureUsable();
      EnsureChildrenCompleted();

      _isRolledBack = true;
      DiagnosticsLogger.Logger.LogInformation("The {TransactionType} with id '{TransactionId}' is rolled back.", TransactionTypeName, TransactionId);
   }

   private void EnsureChildrenCompleted()
   {
      if (_children?.Any(c => !c.IsCompleted) == true)
         throw new InvalidOperationException("Transactions nested incorrectly. At least one of the child transactions is not completed.");
   }

   /// <inheritdoc />
   public abstract bool SupportsSavepoints { get; }

   /// <inheritdoc />
   public abstract void CreateSavepoint(string name);

   /// <inheritdoc />
   public abstract Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);

   /// <inheritdoc />
   public abstract void RollbackToSavepoint(string name);

   /// <inheritdoc />
   public abstract Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);

   /// <inheritdoc />
   public abstract void ReleaseSavepoint(string name);

   /// <inheritdoc />
   public abstract Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);

   /// <inheritdoc />
   public void Dispose()
   {
      if (_isDisposed)
         return;

      DiagnosticsLogger.Logger.LogInformation("Disposing {TransactionType} with id '{TransactionId}'.", TransactionTypeName, TransactionId);
      Dispose(true);

      _isDisposed = true;

      GC.SuppressFinalize(this);
   }

   /// <inheritdoc />
   public async ValueTask DisposeAsync()
   {
      if (_isDisposed)
         return;

      DiagnosticsLogger.Logger.LogInformation("Disposing {TransactionType} with id '{TransactionId}'.", TransactionTypeName, TransactionId);
      await DisposeAsync(true).ConfigureAwait(false);

      _isDisposed = true;

      GC.SuppressFinalize(this);
   }

   /// <summary>
   /// Disposes of current transaction.
   /// </summary>
   /// <param name="disposing">Indication whether this call is made by the method <see cref="Dispose(bool)"/>.</param>
   protected virtual void Dispose(bool disposing)
   {
      if (!disposing)
         return;

      DisposeChildren();

      if (!IsCompleted)
         RollbackInternal();

      NestedTransactionManager.Remove(this);
      NestedTransactionManager = null!;
      _children = null;
   }

   /// <summary>
   /// Disposes of current transaction.
   /// </summary>
   /// <param name="disposing">Indication whether this call is made by the method <see cref="DisposeAsync(bool)"/>.</param>
   protected virtual async ValueTask DisposeAsync(bool disposing)
   {
      if (!disposing)
         return;

      await DisposeChildrenAsync().ConfigureAwait(false);

      if (!IsCompleted)
         RollbackInternal();

      await NestedTransactionManager.RemoveAsync(this).ConfigureAwait(false);
      NestedTransactionManager = null!;
      _children = null;
   }

   private void DisposeChildren()
   {
      if (_children == null)
         return;

      while (_children.Count > 0)
      {
         _children.Pop().Dispose();
      }
   }

   private async ValueTask DisposeChildrenAsync()
   {
      if (_children == null)
         return;

      while (_children.Count > 0)
      {
         await _children.Pop().DisposeAsync().ConfigureAwait(false);
      }
   }

   private void EnsureUsable()
   {
      if (IsCompleted || _isDisposed)
         throw new InvalidOperationException($"This {TransactionTypeName} has completed; it is no longer usable.");
   }
}
