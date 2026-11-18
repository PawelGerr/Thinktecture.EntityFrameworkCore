using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

/// <summary>
/// Reads the member columns of <see cref="TestEntityWithStructsInOptionalComplexType"/> with raw SQL.
/// The shared helper <see cref="TestEntityWithStructsInOptionalComplexType.QueryStructColumns"/> uses double quotes, which MySQL reads as string literals.
/// </summary>
internal static class MySqlStructColumns
{
   public static IQueryable<TestEntityWithStructsInOptionalComplexType.StructColumns> Query(DbContext ctx, string tableName)
   {
      return ctx.Database.SqlQueryRaw<TestEntityWithStructsInOptionalComplexType.StructColumns>($"""
         SELECT `Id`,
                `RequiredPair_ReceivedAt` AS `RequiredPairReceivedAt`,
                `RequiredPair_MessageTimestamp` AS `RequiredPairMessageTimestamp`,
                `OptionalOuter_Y` AS `OptionalOuterY`,
                `OptionalOuter_Pair_ReceivedAt` AS `OptionalOuterPairReceivedAt`,
                `OptionalOuter_Pair_MessageTimestamp` AS `OptionalOuterPairMessageTimestamp`,
                `OptionalOuter_TaggedStruct_Discriminator` AS `OptionalOuterTaggedStructDiscriminator`,
                `OptionalOuter_TaggedStruct_X` AS `OptionalOuterTaggedStructX`,
                `OptionalOuter_OptionalPair_ReceivedAt` AS `OptionalOuterOptionalPairReceivedAt`,
                `OptionalOuter_OptionalPair_MessageTimestamp` AS `OptionalOuterOptionalPairMessageTimestamp`
         FROM `{tableName}`
         """);
   }
}
