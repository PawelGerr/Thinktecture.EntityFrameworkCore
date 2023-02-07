namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Options used for locking the database during migrations and tear down.
/// </summary>
public class SqlServerLockTableOptions
{
   /// <summary>
   /// Indication whether the feature is enabled or not.
   /// </summary>
   public bool IsEnabled { get; }

   /// <summary>
   /// The name of the table.
   /// Default: '__IntegrationTestIsolation'
   /// </summary>
   public string Name { get; set; }

   /// <summary>
   /// The schema of the table.
   /// </summary>
   public string? Schema { get; set; }

   /// <summary>
   /// Number of retries for creation of the table.
   /// Default: 10
   /// </summary>
   public int MaxNumberOfLockRetries { get; set; }

   /// <summary>
   /// Min. delay between retries to create the table.
   /// Default: 50ms
   /// </summary>
   public TimeSpan MinRetryDelay { get; set; }

   /// <summary>
   /// Max. delay between retries to create the table.
   /// Default: 300ms
   /// </summary>
   public TimeSpan MaxRetryDelay { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerLockTableOptions"/>.
   /// </summary>
   /// <param name="isEnabled">Indication whether the feature is enabled or not.</param>
   public SqlServerLockTableOptions(bool isEnabled)
   {
      IsEnabled = isEnabled;
      Name = "__IntegrationTestIsolation";
      MaxNumberOfLockRetries = 10;
      MinRetryDelay = TimeSpan.FromMilliseconds(50);
      MaxRetryDelay = TimeSpan.FromMilliseconds(200);
   }
}
