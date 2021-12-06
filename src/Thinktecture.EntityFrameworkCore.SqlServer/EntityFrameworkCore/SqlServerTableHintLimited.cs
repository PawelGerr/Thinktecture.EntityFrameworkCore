namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// SQL Server table hints.
/// </summary>
public class SqlServerTableHintLimited : IEquatable<SqlServerTableHintLimited>
{
   /// <summary>
   /// KEEPIDENTITY
   /// </summary>
   public static readonly SqlServerTableHintLimited KeepIdentity = new("KEEPIDENTITY");

   /// <summary>
   /// KEEPDEFAULTS
   /// </summary>
   public static readonly SqlServerTableHintLimited KeepDefaults = new("KEEPDEFAULTS");

   /// <summary>
   /// HOLDLOCK
   /// </summary>
   public static readonly SqlServerTableHintLimited HoldLock = new("HOLDLOCK");

   /// <summary>
   /// IGNORE_CONSTRAINTS
   /// </summary>
   public static readonly SqlServerTableHintLimited IgnoreConstraints = new("IGNORE_CONSTRAINTS");

   /// <summary>
   /// IGNORE_TRIGGERS
   /// </summary>
   public static readonly SqlServerTableHintLimited IgnoreTriggers = new("IGNORE_TRIGGERS");

   /// <summary>
   /// NOLOCK
   /// </summary>
   public static readonly SqlServerTableHintLimited NoLock = new("NOLOCK");

   /// <summary>
   /// NOWAIT
   /// </summary>
   public static readonly SqlServerTableHintLimited NoWait = new("NOWAIT");

   /// <summary>
   /// PAGLOCK
   /// </summary>
   public static readonly SqlServerTableHintLimited PagLock = new("PAGLOCK");

   /// <summary>
   /// READCOMMITTED
   /// </summary>
   public static readonly SqlServerTableHintLimited ReadCommitted = new("READCOMMITTED");

   /// <summary>
   /// READCOMMITTEDLOCK
   /// </summary>
   public static readonly SqlServerTableHintLimited ReadCommittedLock = new("READCOMMITTEDLOCK");

   /// <summary>
   /// READPAST
   /// </summary>
   public static readonly SqlServerTableHintLimited ReadPast = new("READPAST");

   /// <summary>
   /// REPEATABLEREAD
   /// </summary>
   public static readonly SqlServerTableHintLimited RepeatableRead = new("REPEATABLEREAD");

   /// <summary>
   /// ROWLOCK
   /// </summary>
   public static readonly SqlServerTableHintLimited RowLock = new("ROWLOCK");

   /// <summary>
   /// SERIALIZABLE
   /// </summary>
   public static readonly SqlServerTableHintLimited Serializable = new("SERIALIZABLE");

   /// <summary>
   /// SNAPSHOT
   /// </summary>
   public static readonly SqlServerTableHintLimited Snapshot = new("SNAPSHOT");

   /// <summary>
   /// TABLOCK
   /// </summary>
   public static readonly SqlServerTableHintLimited TabLock = new("TABLOCK");

   /// <summary>
   /// TABLOCKX
   /// </summary>
   public static readonly SqlServerTableHintLimited TabLockx = new("TABLOCKX");

   /// <summary>
   /// UPDLOCK
   /// </summary>
   public static readonly SqlServerTableHintLimited UpdLock = new("UPDLOCK");

   /// <summary>
   /// XLOCK
   /// </summary>
   public static readonly SqlServerTableHintLimited XLock = new("XLOCK");

   private readonly string _value;

   private SqlServerTableHintLimited(string value)
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
      return Equals((SqlServerTableHintLimited)obj);
   }

   /// <inheritdoc />
   public bool Equals(SqlServerTableHintLimited? other)
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
