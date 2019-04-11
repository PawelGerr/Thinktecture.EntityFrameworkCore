using System;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace Thinktecture.EntityFrameworkCore.Migrations
{
   /// <summary>
   /// An implementation of <see cref="IMigrationsAssembly"/> that is able to instantiate migrations requiring an <see cref="IDbContextSchema"/>.
   /// </summary>
   public class DbSchemaAwareMigrationAssembly : MigrationsAssembly
   {
      private readonly DbContext _context;

      /// <inheritdoc />
      public DbSchemaAwareMigrationAssembly([NotNull] ICurrentDbContext currentContext, [NotNull] IDbContextOptions options,
                                            [NotNull] IMigrationsIdGenerator idGenerator, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
         : base(currentContext, options, idGenerator, logger)
      {
         _context = currentContext.Context;
      }

      /// <inheritdoc />
      public override Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
      {
         if (migrationClass == null)
            throw new ArgumentNullException(nameof(migrationClass));
         if (activeProvider == null)
            throw new ArgumentNullException(nameof(activeProvider));

         var hasCtorWithSchema = migrationClass.GetConstructor(new[] { typeof(IDbContextSchema) }) != null;

         // ReSharper disable once SuspiciousTypeConversion.Global
         if (!hasCtorWithSchema)
            return base.CreateMigration(migrationClass, activeProvider);

         if (!(_context is IDbContextSchema schema))
            throw new ArgumentException($"For instantiation of database schema aware migration of type '{migrationClass.Name}' the database context of type '{_context.GetType().Name}' has to implement the interface {nameof(IDbContextSchema)}.", nameof(migrationClass));

         var instance = (Migration)Activator.CreateInstance(migrationClass.AsType(), schema);
         instance.ActiveProvider = activeProvider;

         return instance;
      }
   }
}
