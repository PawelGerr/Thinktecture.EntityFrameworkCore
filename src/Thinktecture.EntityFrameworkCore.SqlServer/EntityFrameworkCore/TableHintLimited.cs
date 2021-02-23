using System;

namespace Thinktecture.EntityFrameworkCore
{
   /// <summary>
   /// SQL Server table hints.
   /// </summary>
   public class TableHintLimited : IEquatable<TableHintLimited>
   {
      /// <summary>
      /// KEEPIDENTITY
      /// </summary>
      public static readonly TableHintLimited KeepIdentity = new("KEEPIDENTITY");

      /// <summary>
      /// KEEPDEFAULTS
      /// </summary>
      public static readonly TableHintLimited KeepDefaults = new("KEEPDEFAULTS");

      /// <summary>
      /// HOLDLOCK
      /// </summary>
      public static readonly TableHintLimited HoldLock = new("HOLDLOCK");

      /// <summary>
      /// IGNORE_CONSTRAINTS
      /// </summary>
      public static readonly TableHintLimited IgnoreConstraints = new("IGNORE_CONSTRAINTS");

      /// <summary>
      /// IGNORE_TRIGGERS
      /// </summary>
      public static readonly TableHintLimited IgnoreTriggers = new("IGNORE_TRIGGERS");

      /// <summary>
      /// NOLOCK
      /// </summary>
      public static readonly TableHintLimited NoLock = new("NOLOCK");

      /// <summary>
      /// NOWAIT
      /// </summary>
      public static readonly TableHintLimited NoWait = new("NOWAIT");

      /// <summary>
      /// PAGLOCK
      /// </summary>
      public static readonly TableHintLimited PagLock = new("PAGLOCK");

      /// <summary>
      /// READCOMMITTED
      /// </summary>
      public static readonly TableHintLimited ReadCommitted = new("READCOMMITTED");

      /// <summary>
      /// READCOMMITTEDLOCK
      /// </summary>
      public static readonly TableHintLimited ReadCommittedLock = new("READCOMMITTEDLOCK");

      /// <summary>
      /// READPAST
      /// </summary>
      public static readonly TableHintLimited ReadPast = new("READPAST");

      /// <summary>
      /// REPEATABLEREAD
      /// </summary>
      public static readonly TableHintLimited RepeatableRead = new("REPEATABLEREAD");

      /// <summary>
      /// ROWLOCK
      /// </summary>
      public static readonly TableHintLimited RowLock = new("ROWLOCK");

      /// <summary>
      /// SERIALIZABLE
      /// </summary>
      public static readonly TableHintLimited Serializable = new("SERIALIZABLE");

      /// <summary>
      /// SNAPSHOT
      /// </summary>
      public static readonly TableHintLimited Snapshot = new("SNAPSHOT");

      /// <summary>
      /// TABLOCK
      /// </summary>
      public static readonly TableHintLimited TabLock = new("TABLOCK");

      /// <summary>
      /// TABLOCKX
      /// </summary>
      public static readonly TableHintLimited TabLockx = new("TABLOCKX");

      /// <summary>
      /// UPDLOCK
      /// </summary>
      public static readonly TableHintLimited UpdLock = new("UPDLOCK");

      /// <summary>
      /// XLOCK
      /// </summary>
      public static readonly TableHintLimited XLock = new("XLOCK");

      private readonly string _value;

      private TableHintLimited(string value)
      {
         _value = value ?? throw new ArgumentNullException(nameof(value));
      }

      /// <inheritdoc />
      public override bool Equals(object? obj)
      {
         if (ReferenceEquals(null, obj))
            return false;
         if (ReferenceEquals(this, obj))
            return true;
         if (obj.GetType() != this.GetType())
            return false;
         return Equals((TableHintLimited)obj);
      }

      /// <inheritdoc />
      public bool Equals(TableHintLimited? other)
      {
         if (ReferenceEquals(null, other))
            return false;
         if (ReferenceEquals(this, other))
            return true;

         return _value == other._value;
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         return _value.GetHashCode();
      }

      /// <inheritdoc />
      public override string ToString()
      {
         return _value;
      }
   }
}
