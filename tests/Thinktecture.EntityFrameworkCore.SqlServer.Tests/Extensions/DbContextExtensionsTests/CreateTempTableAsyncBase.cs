using System;
using FluentAssertions;
using JetBrains.Annotations;
using Thinktecture.TestDatabaseContext;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   public abstract class CreateTempTableAsyncBase : IntegrationTestsBase
   {
      protected CreateTempTableAsyncBase([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      protected void ValidateColumn([NotNull] InformationSchemaColumn column, string name, string type, bool isNullable, byte? numericPrecision = null, int? numericScale = null, int? charMaxLength = null)
      {
         if (column == null)
            throw new ArgumentNullException(nameof(column));

         column.COLUMN_NAME.Should().Be(name);
         column.DATA_TYPE.Should().Be(type);
         column.IS_NULLABLE.Should().Be(isNullable ? "YES" : "NO");

         if (numericPrecision.HasValue)
            column.NUMERIC_PRECISION.Should().Be(numericPrecision.Value);

         if (numericScale.HasValue)
            column.NUMERIC_SCALE.Should().Be(numericScale.Value);

         if (charMaxLength.HasValue)
            column.CHARACTER_MAXIMUM_LENGTH.Should().Be(charMaxLength.Value);
      }
   }
}
