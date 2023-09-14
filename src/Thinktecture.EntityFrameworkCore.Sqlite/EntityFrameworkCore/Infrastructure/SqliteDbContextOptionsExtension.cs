using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Extensions for DbContextOptions.
/// </summary>
public sealed class SqliteDbContextOptionsExtension : DbContextOptionsExtensionBase, IDbContextOptionsExtension
{
   private readonly RelationalDbContextOptionsExtension _relationalOptions;

   private SqliteDbContextOptionsExtensionInfo? _info;

   /// <inheritdoc />
   public DbContextOptionsExtensionInfo Info => _info ??= new SqliteDbContextOptionsExtensionInfo(this);

   /// <summary>
   /// Enables and disables support for "RowNumber".
   /// </summary>
   public bool AddRowNumberSupport
   {
      get => _relationalOptions.AddRowNumberSupport;
      set => _relationalOptions.AddRowNumberSupport = value;
   }

   private bool _addCustomQueryableMethodTranslatingExpressionVisitorFactory;

   /// <summary>
   /// A custom factory is registered if <c>true</c>.
   /// The factory is required to be able to translate custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
   /// </summary>
   public bool AddCustomQueryableMethodTranslatingExpressionVisitorFactory
   {
      get => _addCustomQueryableMethodTranslatingExpressionVisitorFactory || AddBulkOperationSupport || AddRowNumberSupport;
      set => _addCustomQueryableMethodTranslatingExpressionVisitorFactory = value;
   }

   private bool _addCustomRelationalParameterBasedSqlProcessorFactory;

   /// <summary>
   /// A custom factory is registered if <c>true</c>.
   /// The factory is required for some features.
   /// </summary>
   public bool AddCustomRelationalParameterBasedSqlProcessorFactory
   {
      get => _addCustomRelationalParameterBasedSqlProcessorFactory || AddBulkOperationSupport || AddRowNumberSupport;
      set => _addCustomRelationalParameterBasedSqlProcessorFactory = value;
   }

   private bool _addCustomQuerySqlGeneratorFactory;

   /// <summary>
   /// A custom factory is registered if <c>true</c>.
   /// The factory is required for some features like for generation of 'DELETE' statements.
   /// </summary>
   public bool AddCustomQuerySqlGeneratorFactory
   {
      get => _addCustomQuerySqlGeneratorFactory || AddBulkOperationSupport;
      set => _addCustomQuerySqlGeneratorFactory = value;
   }

   /// <summary>
   /// Enables and disables support for bulk operations and temp tables.
   /// </summary>
   public bool AddBulkOperationSupport { get; set; }

   /// <summary>
   /// Indication whether to configure temp tables for primitive types.
   /// </summary>
   public bool ConfigureTempTablesForPrimitiveTypes { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteDbContextOptionsExtension"/>.
   /// </summary>
   /// <param name="relationalOptions">An instance of <see cref="RelationalDbContextOptionsExtension"/>.</param>
   public SqliteDbContextOptionsExtension(RelationalDbContextOptionsExtension relationalOptions)
   {
      _relationalOptions = relationalOptions ?? throw new ArgumentNullException(nameof(relationalOptions));
   }

   /// <inheritdoc />
   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   public void ApplyServices(IServiceCollection services)
   {
      services.TryAddSingleton<SqliteDbContextOptionsExtensionOptions>();
      services.AddSingleton<IBulkOperationsDbContextOptionsExtensionOptions>(provider => provider.GetRequiredService<SqliteDbContextOptionsExtensionOptions>());
      services.AddSingleton<ISingletonOptions>(provider => provider.GetRequiredService<SqliteDbContextOptionsExtensionOptions>());

      if (AddCustomQueryableMethodTranslatingExpressionVisitorFactory)
         AddWithCheck<IQueryableMethodTranslatingExpressionVisitorFactory, ThinktectureSqliteQueryableMethodTranslatingExpressionVisitorFactory, SqliteQueryableMethodTranslatingExpressionVisitorFactory>(services);

      if (AddCustomQuerySqlGeneratorFactory)
         AddWithCheck<IQuerySqlGeneratorFactory, ThinktectureSqliteQuerySqlGeneratorFactory, SqliteQuerySqlGeneratorFactory>(services);

      if (AddCustomRelationalParameterBasedSqlProcessorFactory)
         AddWithCheck<IRelationalParameterBasedSqlProcessorFactory, ThinktectureSqliteParameterBasedSqlProcessorFactory, SqliteParameterBasedSqlProcessorFactory>(services);

      if (AddBulkOperationSupport)
      {
         services.Add<IConventionSetPlugin, BulkOperationConventionSetPlugin>(GetLifetime<IConventionSetPlugin>());

         services.AddSingleton<TempTableStatementCache<SqliteTempTableCreatorCacheKey>>();
         services.TryAddScoped<ITempTableCreator, SqliteTempTableCreator>();
         services.AddTempTableSuffixComponents();

         AddEntityDataReader(services);
         services.TryAddScoped<SqliteBulkOperationExecutor>();
         services.TryAddScoped<IBulkInsertExecutor>(provider => provider.GetRequiredService<SqliteBulkOperationExecutor>());
         services.TryAddScoped<ITempTableBulkInsertExecutor>(provider => provider.GetRequiredService<SqliteBulkOperationExecutor>());
         services.TryAddScoped<IBulkUpdateExecutor>(provider => provider.GetRequiredService<SqliteBulkOperationExecutor>());
         services.TryAddScoped<IBulkInsertOrUpdateExecutor>(provider => provider.GetRequiredService<SqliteBulkOperationExecutor>());
         services.TryAddScoped<ITruncateTableExecutor>(provider => provider.GetRequiredService<SqliteBulkOperationExecutor>());
      }
   }

   /// <inheritdoc />
   public void Validate(IDbContextOptions options)
   {
   }

   private class SqliteDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
   {
      private readonly SqliteDbContextOptionsExtension _extension;
      public override bool IsDatabaseProvider => false;

      private string? _logFragment;

      public override string LogFragment => _logFragment ??= CreateLogFragment();

      private string CreateLogFragment()
      {
         return _extension.AddBulkOperationSupport ? "BulkOperationSupport " : String.Empty;
      }

      /// <inheritdoc />
      public SqliteDbContextOptionsExtensionInfo(SqliteDbContextOptionsExtension extension)
         : base(extension)
      {
         _extension = extension ?? throw new ArgumentNullException(nameof(extension));
      }

      /// <inheritdoc />
      public override int GetServiceProviderHashCode()
      {
         return HashCode.Combine(_extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory,
                                 _extension.AddCustomQuerySqlGeneratorFactory,
                                 _extension.AddCustomRelationalParameterBasedSqlProcessorFactory,
                                 _extension.AddBulkOperationSupport,
                                 _extension.ConfigureTempTablesForPrimitiveTypes);
      }

      /// <inheritdoc />
      public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
      {
         return other is SqliteDbContextOptionsExtensionInfo otherSqliteInfo
                && _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory == otherSqliteInfo._extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory
                && _extension.AddCustomQuerySqlGeneratorFactory == otherSqliteInfo._extension.AddCustomQuerySqlGeneratorFactory
                && _extension.AddCustomRelationalParameterBasedSqlProcessorFactory == otherSqliteInfo._extension.AddCustomRelationalParameterBasedSqlProcessorFactory
                && _extension.AddBulkOperationSupport == otherSqliteInfo._extension.AddBulkOperationSupport
                && _extension.ConfigureTempTablesForPrimitiveTypes == otherSqliteInfo._extension.ConfigureTempTablesForPrimitiveTypes;
      }

      /// <inheritdoc />
      public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
      {
         debugInfo["Thinktecture:CustomQueryableMethodTranslatingExpressionVisitorFactory"] = _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:CustomQuerySqlGeneratorFactory"] = _extension.AddCustomQuerySqlGeneratorFactory.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:CustomRelationalParameterBasedSqlProcessorFactory"] = _extension.AddCustomRelationalParameterBasedSqlProcessorFactory.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:BulkOperationSupport"] = _extension.AddBulkOperationSupport.ToString(CultureInfo.InvariantCulture);
      }
   }
}
