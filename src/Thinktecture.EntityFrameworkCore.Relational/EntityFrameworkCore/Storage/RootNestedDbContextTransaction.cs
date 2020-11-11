using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Storage
{
   /// <summary>
   /// A root transaction.
   /// </summary>
   public sealed class RootNestedDbContextTransaction : NestedDbContextTransaction
   {
      private readonly IRelationalTransactionManager _innerManager;
      private readonly IDbContextTransaction _innerTx;

      /// <summary>
      /// Initializes new instance of <see cref="RootNestedDbContextTransaction"/>.
      /// </summary>
      /// <param name="logger">Logger.</param>
      /// <param name="nestedTransactionManager">Nested transaction manager.</param>
      /// <param name="innerManager">Inner transaction manager.</param>
      /// <param name="tx">The real transaction.</param>
      /// <param name="transactionId">The transaction id.</param>
      public RootNestedDbContextTransaction(
         IDiagnosticsLogger<RelationalDbLoggerCategory.NestedTransaction> logger,
         NestedRelationalTransactionManager nestedTransactionManager,
         IRelationalTransactionManager innerManager,
         IDbContextTransaction tx,
         Guid? transactionId)
         : base(logger, nestedTransactionManager, transactionId ?? tx?.TransactionId ?? throw new ArgumentNullException(nameof(tx)))
      {
         _innerManager = innerManager ?? throw new ArgumentNullException(nameof(innerManager));
         _innerTx = tx;
      }

      /// <inheritdoc />
      protected internal override DbTransaction GetUnderlyingTransaction()
      {
         return _innerTx.GetDbTransaction();
      }

      /// <inheritdoc />
      public override void Commit()
      {
         base.Commit();

         _innerManager.CommitTransaction();
      }

      /// <inheritdoc />
      public override async Task CommitAsync(CancellationToken cancellationToken = default)
      {
         await base.CommitAsync(cancellationToken).ConfigureAwait(false);

         _innerManager.CommitTransaction();
      }

      /// <inheritdoc />
      public override void Rollback()
      {
         base.Rollback();

         _innerManager.RollbackTransaction();
      }

      /// <inheritdoc />
      public override async Task RollbackAsync(CancellationToken cancellationToken = default)
      {
         await base.RollbackAsync(cancellationToken).ConfigureAwait(false);

         _innerManager.RollbackTransaction();
      }

      /// <inheritdoc />
      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);

         if (disposing)
            _innerTx.Dispose();
      }

      /// <inheritdoc />
      protected override async ValueTask DisposeAsync(bool disposing)
      {
         await base.DisposeAsync(disposing).ConfigureAwait(false);

         if (disposing)
            await _innerTx.DisposeAsync().ConfigureAwait(false);
      }
   }
}
