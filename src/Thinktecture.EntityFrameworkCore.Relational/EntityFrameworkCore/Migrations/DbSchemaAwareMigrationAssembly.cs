using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Thinktecture.EntityFrameworkCore.Migrations
{
   /// <summary>
   /// An implementation of <see cref="IMigrationsAssembly"/> that is able to instantiate migrations requiring an <see cref="IDbDefaultSchema"/>.
   /// </summary>
   public class DbSchemaAwareMigrationAssembly<TMigrationsAssembly> : IMigrationsAssembly
      where TMigrationsAssembly : class, IMigrationsAssembly
   {
      private readonly TMigrationsAssembly _innerMigrationsAssembly;
      private readonly IMigrationOperationSchemaSetter _schemaSetter;
      private readonly DbContext _context;

      /// <inheritdoc />
      public IReadOnlyDictionary<string, TypeInfo> Migrations => _innerMigrationsAssembly.Migrations;

      /// <inheritdoc />
      public ModelSnapshot ModelSnapshot => _innerMigrationsAssembly.ModelSnapshot;

      /// <inheritdoc />
      public Assembly Assembly => _innerMigrationsAssembly.Assembly;

      /// <inheritdoc />
      public DbSchemaAwareMigrationAssembly([NotNull] TMigrationsAssembly migrationsAssembly,
                                            [NotNull] IMigrationOperationSchemaSetter schemaSetter,
                                            [NotNull] ICurrentDbContext currentContext)
      {
         _innerMigrationsAssembly = migrationsAssembly ?? throw new ArgumentNullException(nameof(migrationsAssembly));
         _schemaSetter = schemaSetter ?? throw new ArgumentNullException(nameof(schemaSetter));
         // ReSharper disable once ConstantConditionalAccessQualifier
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

         var isSchemaAwareMigration = migrationClass.GetConstructor(new[] { typeof(IDbDefaultSchema) }) != null;

         // is schema-aware context
         if (_context is IDbDefaultSchema schema)
         {
            var migration = isSchemaAwareMigration
                               ? CreateSchemaAwareMigration(migrationClass, activeProvider, schema)
                               : _innerMigrationsAssembly.CreateMigration(migrationClass, activeProvider);

            SetSchema(migration.UpOperations, schema);
            SetSchema(migration.DownOperations, schema);

            return migration;
         }

         if (!isSchemaAwareMigration)
            return _innerMigrationsAssembly.CreateMigration(migrationClass, activeProvider);

         throw new ArgumentException($"For instantiation of schema-aware migration of type '{migrationClass.Name}' the database context of type '{_context.GetType().DisplayName()}' has to implement the interface '{nameof(IDbDefaultSchema)}'.", nameof(migrationClass));
      }

      private void SetSchema([NotNull] IReadOnlyList<MigrationOperation> operations, [CanBeNull] IDbDefaultSchema schema)
      {
         if (schema?.Schema != null)
            _schemaSetter.SetSchema(operations, schema.Schema);
      }

      [NotNull]
      private static Migration CreateSchemaAwareMigration([NotNull] TypeInfo migrationClass, [NotNull] string activeProvider, [NotNull] IDbDefaultSchema schema)
      {
         var migration = (Migration)Activator.CreateInstance(migrationClass.AsType(), schema);
         migration.ActiveProvider = activeProvider;

         return migration;
      }
   }
}
