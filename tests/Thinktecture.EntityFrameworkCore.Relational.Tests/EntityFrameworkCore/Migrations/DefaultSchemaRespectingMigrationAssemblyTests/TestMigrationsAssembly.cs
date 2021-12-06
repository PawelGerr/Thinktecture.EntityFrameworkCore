using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Moq;

namespace Thinktecture.EntityFrameworkCore.Migrations.DefaultSchemaRespectingMigrationAssemblyTests;

public class TestMigrationsAssembly : IMigrationsAssembly
{
   private readonly IMigrationsAssembly _migrationsAssembly;

   public Mock<IMigrationsAssembly> Mock { get; }

   public TestMigrationsAssembly()
   {
      Mock = new Mock<IMigrationsAssembly>();
      _migrationsAssembly = Mock.Object;
   }

   /// <inheritdoc />
   public string? FindMigrationId(string nameOrId)
   {
      return _migrationsAssembly.FindMigrationId(nameOrId);
   }

   /// <inheritdoc />
   public Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
   {
      return _migrationsAssembly.CreateMigration(migrationClass, activeProvider);
   }

   /// <inheritdoc />
   public IReadOnlyDictionary<string, TypeInfo> Migrations => _migrationsAssembly.Migrations;

   /// <inheritdoc />
   public ModelSnapshot? ModelSnapshot => _migrationsAssembly.ModelSnapshot;

   /// <inheritdoc />
   public Assembly Assembly => _migrationsAssembly.Assembly;
}