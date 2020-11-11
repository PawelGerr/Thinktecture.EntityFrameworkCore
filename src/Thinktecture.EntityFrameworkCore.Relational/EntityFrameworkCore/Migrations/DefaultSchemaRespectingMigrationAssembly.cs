using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace Thinktecture.EntityFrameworkCore.Migrations
{
   /// <summary>
   /// An implementation of <see cref="IMigrationsAssembly"/> that is able to instantiate migrations requiring an <see cref="IDbDefaultSchema"/>.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
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
      public ModelSnapshot ModelSnapshot => _innerMigrationsAssembly.ModelSnapshot;

      /// <inheritdoc />
      public Assembly Assembly => _innerMigrationsAssembly.Assembly;

      /// <summary>
      /// Initializes new instance of <see cref="DefaultSchemaRespectingMigrationAssembly{TMigrationsAssembly}"/>.
      /// </summary>
      public DefaultSchemaRespectingMigrationAssembly(TMigrationsAssembly migrationsAssembly,
                                                      IMigrationOperationSchemaSetter schemaSetter,
                                                      ICurrentDbContext currentContext,
                                                      IServiceProvider serviceProvider)
      {
         _innerMigrationsAssembly = migrationsAssembly ?? throw new ArgumentNullException(nameof(migrationsAssembly));
         _schemaSetter = schemaSetter ?? throw new ArgumentNullException(nameof(schemaSetter));
         _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
         _context = currentContext?.Context ?? throw new ArgumentNullException(nameof(currentContext));
      }

      /// <inheritdoc />
      public string FindMigrationId(string nameOrId)
      {
         return _innerMigrationsAssembly.FindMigrationId(nameOrId);
      }

      /// <inheritdoc />
      public Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
      {
         if (migrationClass == null)
            throw new ArgumentNullException(nameof(migrationClass));
         if (activeProvider == null)
            throw new ArgumentNullException(nameof(activeProvider));

         var hasCtorWithDefaultSchema = migrationClass.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(IDbDefaultSchema)));

         // has default schema
         if (_context is IDbDefaultSchema schema)
         {
            var migration = hasCtorWithDefaultSchema ? CreateInstance(migrationClass, schema, activeProvider) : CreateInstance(migrationClass, activeProvider);

            SetSchema(migration.UpOperations, schema);
            SetSchema(migration.DownOperations, schema);

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

      private void SetSchema(IReadOnlyList<MigrationOperation> operations, [AllowNull] IDbDefaultSchema schema)
      {
         if (schema?.Schema != null)
            _schemaSetter.SetSchema(operations, schema.Schema);
      }
   }
}
