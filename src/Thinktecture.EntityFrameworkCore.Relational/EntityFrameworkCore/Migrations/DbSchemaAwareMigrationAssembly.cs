using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.EntityFrameworkCore.Migrations
{
   /// <summary>
   /// An implementation of <see cref="IMigrationsAssembly"/> that is able to instantiate migrations requiring an <see cref="IDbContextSchema"/>.
   /// </summary>
   public class DbSchemaAwareMigrationAssembly<TMigrationsAssembly> : IMigrationsAssembly
      where TMigrationsAssembly : class, IMigrationsAssembly
   {
      private readonly TMigrationsAssembly _innerMigrationsAssembly;
      private readonly DbContext _context;

      /// <inheritdoc />
      public IReadOnlyDictionary<string, TypeInfo> Migrations => _innerMigrationsAssembly.Migrations;

      /// <inheritdoc />
      public ModelSnapshot ModelSnapshot => _innerMigrationsAssembly.ModelSnapshot;

      /// <inheritdoc />
      public Assembly Assembly => _innerMigrationsAssembly.Assembly;

      /// <inheritdoc />
      public DbSchemaAwareMigrationAssembly([NotNull] TMigrationsAssembly migrationsAssembly, [NotNull] ICurrentDbContext currentContext)
      {
         _innerMigrationsAssembly = migrationsAssembly ?? throw new ArgumentNullException(nameof(migrationsAssembly));
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

         var hasCtorWithSchema = migrationClass.GetConstructor(new[] { typeof(IDbContextSchema) }) != null;

         // ReSharper disable once SuspiciousTypeConversion.Global
         if (!hasCtorWithSchema)
            return _innerMigrationsAssembly.CreateMigration(migrationClass, activeProvider);

         if (!(_context is IDbContextSchema schema))
            throw new ArgumentException($"For instantiation of schema-aware migration of type '{migrationClass.Name}' the database context of type '{_context.GetType().DisplayName()}' has to implement the interface '{nameof(IDbContextSchema)}'.", nameof(migrationClass));

         var instance = (Migration)Activator.CreateInstance(migrationClass.AsType(), schema);
         instance.ActiveProvider = activeProvider;

         return instance;
      }
   }
}
