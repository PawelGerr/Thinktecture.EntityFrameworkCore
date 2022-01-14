using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Thinktecture.EntityFrameworkCore.Migrations;

/// <summary>
/// An implementation of <see cref="IMigrationsAssembly"/> that is able to instantiate migrations requiring an <see cref="IDbDefaultSchema"/>.
/// </summary>
public sealed class DefaultSchemaRespectingMigrationAssembly<TMigrationsAssembly> : IMigrationsAssembly
   where TMigrationsAssembly : class, IMigrationsAssembly
{
   private readonly TMigrationsAssembly _innerMigrationsAssembly;
   private readonly IMigrationOperationSchemaSetter _schemaSetter;
   private readonly IServiceProvider _serviceProvider;
   private readonly DbContext _context;

   /// <inheritdoc />
   public IReadOnlyDictionary<string, TypeInfo> Migrations => _innerMigrationsAssembly.Migrations;

   /// <inheritdoc />
   public ModelSnapshot? ModelSnapshot => _innerMigrationsAssembly.ModelSnapshot;

   /// <inheritdoc />
   public Assembly Assembly => _innerMigrationsAssembly.Assembly;

   /// <summary>
   /// Initializes new instance of <see cref="DefaultSchemaRespectingMigrationAssembly{TMigrationsAssembly}"/>.
   /// </summary>
   public DefaultSchemaRespectingMigrationAssembly(
      TMigrationsAssembly migrationsAssembly,
      IMigrationOperationSchemaSetter schemaSetter,
      ICurrentDbContext currentContext,
      IServiceProvider serviceProvider)
   {
      ArgumentNullException.ThrowIfNull(currentContext);

      _innerMigrationsAssembly = migrationsAssembly ?? throw new ArgumentNullException(nameof(migrationsAssembly));
      _schemaSetter = schemaSetter ?? throw new ArgumentNullException(nameof(schemaSetter));
      _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
      _context = currentContext.Context ?? throw new ArgumentNullException(nameof(currentContext));
   }

   /// <inheritdoc />
   public string? FindMigrationId(string nameOrId)
   {
      return _innerMigrationsAssembly.FindMigrationId(nameOrId);
   }

   /// <inheritdoc />
   public Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
   {
      ArgumentNullException.ThrowIfNull(migrationClass);
      ArgumentNullException.ThrowIfNull(activeProvider);

      var hasCtorWithDefaultSchema = migrationClass.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(IDbDefaultSchema)));

      // ReSharper disable once SuspiciousTypeConversion.Global
      if (_context is IDbDefaultSchema schema)
      {
         var migration = hasCtorWithDefaultSchema ? CreateInstance(migrationClass, schema, activeProvider) : CreateInstance(migrationClass, activeProvider);

         if (schema.Schema is not null)
         {
            _schemaSetter.SetSchema(migration.UpOperations, schema.Schema);
            _schemaSetter.SetSchema(migration.DownOperations, schema.Schema);
         }

         return migration;
      }

      if (!hasCtorWithDefaultSchema)
         return CreateInstance(migrationClass, activeProvider);

      throw new ArgumentException($"For instantiation of default schema respecting migration of type '{migrationClass.Name}' the database context of type '{_context.GetType().ShortDisplayName()}' has to implement the interface '{nameof(IDbDefaultSchema)}'.", nameof(migrationClass));
   }

   private Migration CreateInstance(TypeInfo migrationClass, IDbDefaultSchema schema, string activeProvider)
   {
      var migration = (Migration)ActivatorUtilities.CreateInstance(_serviceProvider, migrationClass.AsType(), schema);
      migration.ActiveProvider = activeProvider;

      return migration;
   }

   private Migration CreateInstance(TypeInfo migrationClass, string activeProvider)
   {
      var migration = (Migration)ActivatorUtilities.CreateInstance(_serviceProvider, migrationClass.AsType());
      migration.ActiveProvider = activeProvider;

      return migration;
   }
}
