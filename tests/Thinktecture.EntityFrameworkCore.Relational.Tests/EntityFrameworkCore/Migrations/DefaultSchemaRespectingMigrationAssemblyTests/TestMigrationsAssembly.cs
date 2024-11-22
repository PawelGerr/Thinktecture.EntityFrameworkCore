using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.EntityFrameworkCore.Migrations.DefaultSchemaRespectingMigrationAssemblyTests;

public class TestMigrationsAssembly : IMigrationsAssembly
{
   public IMigrationsAssembly Mock { get; } = Substitute.For<IMigrationsAssembly>();

   /// <inheritdoc />
   public string? FindMigrationId(string nameOrId)
   {
      return Mock.FindMigrationId(nameOrId);
   }

   /// <inheritdoc />
   public Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
   {
      return Mock.CreateMigration(migrationClass, activeProvider);
   }

   /// <inheritdoc />
   public IReadOnlyDictionary<string, TypeInfo> Migrations => Mock.Migrations;

   /// <inheritdoc />
   public ModelSnapshot? ModelSnapshot => Mock.ModelSnapshot;

   /// <inheritdoc />
   public Assembly Assembly => Mock.Assembly;
}
