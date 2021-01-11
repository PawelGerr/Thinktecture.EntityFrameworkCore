using System;
using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table
   /// </summary>
   public class TempTableCreationOptions : ITempTableCreationOptions
   {
      /// <inheritdoc />
      public bool TruncateTableIfExists { get; set; }

      private ITempTableNameProvider? _tableNameProvider;

      /// <inheritdoc />
      public ITempTableNameProvider TableNameProvider
      {
         get => _tableNameProvider ?? ReusingTempTableNameProvider.Instance;
         set => _tableNameProvider = value ?? throw new ArgumentNullException(nameof(value), "The table name provider cannot be null.");
      }

      private IPrimaryKeyPropertiesProvider? _primaryKeyCreation;

      /// <inheritdoc />
      public IPrimaryKeyPropertiesProvider PrimaryKeyCreation
      {
         get => _primaryKeyCreation ?? PrimaryKeyPropertiesProviders.EntityTypeConfiguration;
         set => _primaryKeyCreation = value ?? throw new ArgumentNullException(nameof(value), "The primary key column provider cannot be null.");
      }

      /// <inheritdoc />
      public IEntityMembersProvider? MembersToInclude { get; set; }

      /// <inheritdoc />
      public bool DropTableOnDispose { get; set; }

      /// <summary>
      /// Initializes a new instance of <see cref="TempTableCreationOptions"/>.
      /// </summary>
      /// <param name="options">Options to take values over.</param>
      public TempTableCreationOptions(ITempTableCreationOptions? options = null)
      {
         if (options is null)
         {
            DropTableOnDispose = true;
         }
         else
         {
            InitializeFrom(options);
         }
      }

      private void InitializeFrom(ITempTableCreationOptions options)
      {
         PrimaryKeyCreation = options.PrimaryKeyCreation;
         TableNameProvider = options.TableNameProvider;
         TruncateTableIfExists = options.TruncateTableIfExists;
         MembersToInclude = options.MembersToInclude;
         DropTableOnDispose = options.DropTableOnDispose;
      }
   }
}
