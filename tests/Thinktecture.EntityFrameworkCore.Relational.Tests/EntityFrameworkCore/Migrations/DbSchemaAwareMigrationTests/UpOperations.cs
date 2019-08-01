using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationTests
{
   public class UpOperations : OperationsBase
   {
      [NotNull]
      protected override IReadOnlyList<MigrationOperation> Operations => SUT.UpOperations;

      protected override Action<MigrationBuilder> Configure
      {
         get => SUT.ConfigureUp;
         set => SUT.ConfigureUp = value;
      }

      public UpOperations([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }
   }
}
