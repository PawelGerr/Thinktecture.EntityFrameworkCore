using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Options required for creation of a temp table
/// </summary>
public class TempTableCreationOptions : ITempTableCreationOptions
{
   /// <inheritdoc />
   public bool TruncateTableIfExists { get; set; }

   private ITempTableNameProvider? _tableNameProvider;

   /// <inheritdoc />
   [AllowNull]
   public ITempTableNameProvider TableNameProvider
   {
      get => _tableNameProvider ?? ReusingTempTableNameProvider.Instance;
      set => _tableNameProvider = value;
   }

   private IPrimaryKeyPropertiesProvider? _primaryKeyCreation;

   /// <inheritdoc />
   [AllowNull]
   public IPrimaryKeyPropertiesProvider PrimaryKeyCreation
   {
      get => _primaryKeyCreation ?? PrimaryKeyPropertiesProviders.EntityTypeConfiguration;
      set => _primaryKeyCreation = value;
   }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInclude { get; set; }

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
      PropertiesToInclude = options.PropertiesToInclude;
      DropTableOnDispose = options.DropTableOnDispose;
   }
}
