using System;
using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table
   /// </summary>
   public sealed class TempTableCreationOptions : ITempTableCreationOptions
   {
      /// <inheritdoc />
      public bool DropTempTableIfExists { get; set; }

      private ITempTableNameProvider _tableNameProvider;

      /// <inheritdoc />
      public ITempTableNameProvider TableNameProvider
      {
         get => _tableNameProvider ?? NewGuidTempTableNameProvider.Instance; // TODO
         set => _tableNameProvider = value ?? throw new ArgumentNullException(nameof(value), "The table name provider cannot be null.");
      }

      /// <inheritdoc />
      public bool CreatePrimaryKey { get; set; } = true;

      /// <inheritdoc />
      public IEntityMembersProvider? MembersToInclude { get; set; }

      /// <summary>
      /// Initializes a new instance of <see cref="TempTableCreationOptions"/>.
      /// </summary>
      public TempTableCreationOptions()
      {
      }

      /// <summary>
      /// Initializes a new instance of <see cref="TempTableCreationOptions"/>
      /// and takes over values from the provided <paramref name="options"/>.
      /// </summary>
      /// <param name="options">Options to take values from</param>
      /// <exception cref="ArgumentNullException">
      /// if <paramref name="options"/> is <c>null</c>.
      /// </exception>
      public TempTableCreationOptions(ITempTableCreationOptions options)
      {
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         CreatePrimaryKey = options.CreatePrimaryKey;
         TableNameProvider = options.TableNameProvider;
         DropTempTableIfExists = options.DropTempTableIfExists;
         MembersToInclude = options.MembersToInclude;
      }
   }
}
