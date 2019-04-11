using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationTests
{
   public class DownOperations : OperationsBase
   {
      [NotNull]
      protected override IReadOnlyList<MigrationOperation> Operations => SUT.DownOperations;

      /// <inheritdoc />
      protected override Action<MigrationBuilder> Configure
      {
         get => SUT.ConfigureDown;
         set => SUT.ConfigureDown = value;
      }
   }
}