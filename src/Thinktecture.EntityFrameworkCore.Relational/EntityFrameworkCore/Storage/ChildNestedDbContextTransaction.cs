using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Thinktecture.EntityFrameworkCore.Storage;

/// <summary>
/// A child transaction.
/// </summary>
public class ChildNestedDbContextTransaction : NestedDbContextTransaction
{
   private readonly NestedDbContextTransaction _parent;

   /// <inheritdoc />
   protected override string TransactionTypeName => "child transaction";

   /// <inheritdoc />
   public override bool SupportsSavepoints => _parent.SupportsSavepoints;

   /// <summary>
   /// Initializes new instance of <see cref="ChildNestedDbContextTransaction"/>.
   /// </summary>
   /// <param name="logger">Logger.</param>
   /// <param name="nestedTransactionManager">Nested transaction manager.</param>
   /// <param name="parent">Parent transaction.</param>
   public ChildNestedDbContextTransaction(
      IDiagnosticsLogger<RelationalDbLoggerCategory.NestedTransaction> logger,
      NestedRelationalTransactionManager nestedTransactionManager,
      NestedDbContextTransaction parent)
      : base(logger, nestedTransactionManager, Guid.NewGuid())
   {
      _parent = parent ?? throw new ArgumentNullException(nameof(parent));
   }

   /// <inheritdoc />
   protected internal override DbTransaction GetUnderlyingTransaction()
   {
      return _parent.GetUnderlyingTransaction();
   }

   /// <inheritdoc />
   public override void CreateSavepoint(string name)
   {
      _parent.CreateSavepoint(name);
   }

   /// <inheritdoc />
   public override Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
   {
      return _parent.CreateSavepointAsync(name, cancellationToken);
   }

   /// <inheritdoc />
   public override void RollbackToSavepoint(string name)
   {
      _parent.RollbackToSavepoint(name);
   }

   /// <inheritdoc />
   public override Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
   {
      return _parent.RollbackToSavepointAsync(name, cancellationToken);
   }

   /// <inheritdoc />
   public override void ReleaseSavepoint(string name)
   {
      _parent.ReleaseSavepoint(name);
   }

   /// <inheritdoc />
   public override Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
   {
      return _parent.ReleaseSavepointAsync(name, cancellationToken);
   }
}