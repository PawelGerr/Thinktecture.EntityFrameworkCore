namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithStructsInOptionalComplexType
{
   public Guid Id { get; set; }
   public TimestampPair RequiredPair { get; set; }
   public OuterWithStructs? OptionalOuter { get; set; }

   public static TestEntityWithStructsInOptionalComplexType CreateWithValues(Guid id) => new()
                                                                                        {
                                                                                           Id = id,
                                                                                           RequiredPair = new TimestampPair(1, 2),
                                                                                           OptionalOuter = new OuterWithStructs
                                                                                                           {
                                                                                                              Y = 3,
                                                                                                              Pair = new TimestampPair(300, 400),
                                                                                                              TaggedStruct = new TaggedStruct(7),
                                                                                                              OptionalPair = new TimestampPair(500, 600)
                                                                                                           }
                                                                                        };

   public static TestEntityWithStructsInOptionalComplexType CreateWithNulls(Guid id) => new() { Id = id, RequiredPair = new TimestampPair(1, 2) };

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithStructsInOptionalComplexType>(builder =>
                                                                      {
                                                                         builder.ToTable("TestEntitiesWithStructsInOptionalComplexType");
                                                                         builder.ComplexProperty(e => e.RequiredPair);
                                                                         builder.ComplexProperty(e => e.OptionalOuter, b =>
                                                                                                                       {
                                                                                                                          b.ComplexProperty(o => o.Pair);
                                                                                                                          b.ComplexProperty(o => o.TaggedStruct, t => t.HasDiscriminator());
                                                                                                                          b.ComplexProperty(o => o.OptionalPair);
                                                                                                                       });
                                                                      });
   }

   /// <summary>
   /// The columns of the struct complex properties, read with raw SQL.
   /// EF Core materializes a null container from the other columns, so a read through the entity cannot detect a written default value.
   /// </summary>
   public sealed class StructColumns
   {
      public Guid Id { get; set; }
      public long? RequiredPairReceivedAt { get; set; }
      public long? RequiredPairMessageTimestamp { get; set; }
      public int? OptionalOuterY { get; set; }
      public long? OptionalOuterPairReceivedAt { get; set; }
      public long? OptionalOuterPairMessageTimestamp { get; set; }
      public string? OptionalOuterTaggedStructDiscriminator { get; set; }
      public int? OptionalOuterTaggedStructX { get; set; }
      public long? OptionalOuterOptionalPairReceivedAt { get; set; }
      public long? OptionalOuterOptionalPairMessageTimestamp { get; set; }

      /// <summary>
      /// The values that SaveChanges writes for the entity: NULL for every column below a null container, the discriminator value for a non-null container.
      /// </summary>
      public static StructColumns From(TestEntityWithStructsInOptionalComplexType entity) => new()
                                                                                            {
                                                                                               Id = entity.Id,
                                                                                               RequiredPairReceivedAt = entity.RequiredPair.ReceivedAt,
                                                                                               RequiredPairMessageTimestamp = entity.RequiredPair.MessageTimestamp,
                                                                                               OptionalOuterY = entity.OptionalOuter?.Y,
                                                                                               OptionalOuterPairReceivedAt = entity.OptionalOuter?.Pair.ReceivedAt,
                                                                                               OptionalOuterPairMessageTimestamp = entity.OptionalOuter?.Pair.MessageTimestamp,
                                                                                               OptionalOuterTaggedStructDiscriminator = entity.OptionalOuter is null ? null : nameof(TaggedStruct),
                                                                                               OptionalOuterTaggedStructX = entity.OptionalOuter?.TaggedStruct.X,
                                                                                               OptionalOuterOptionalPairReceivedAt = entity.OptionalOuter?.OptionalPair?.ReceivedAt,
                                                                                               OptionalOuterOptionalPairMessageTimestamp = entity.OptionalOuter?.OptionalPair?.MessageTimestamp
                                                                                            };
   }

   /// <summary>
   /// Reads the struct member columns of the table <paramref name="quotedTableName"/> (delimited with double quotes, with schema when needed).
   /// </summary>
   public static IQueryable<StructColumns> QueryStructColumns(DbContext ctx, string quotedTableName)
   {
      return ctx.Database.SqlQueryRaw<StructColumns>($"""
         SELECT "Id",
                "RequiredPair_ReceivedAt" AS "RequiredPairReceivedAt",
                "RequiredPair_MessageTimestamp" AS "RequiredPairMessageTimestamp",
                "OptionalOuter_Y" AS "OptionalOuterY",
                "OptionalOuter_Pair_ReceivedAt" AS "OptionalOuterPairReceivedAt",
                "OptionalOuter_Pair_MessageTimestamp" AS "OptionalOuterPairMessageTimestamp",
                "OptionalOuter_TaggedStruct_Discriminator" AS "OptionalOuterTaggedStructDiscriminator",
                "OptionalOuter_TaggedStruct_X" AS "OptionalOuterTaggedStructX",
                "OptionalOuter_OptionalPair_ReceivedAt" AS "OptionalOuterOptionalPairReceivedAt",
                "OptionalOuter_OptionalPair_MessageTimestamp" AS "OptionalOuterOptionalPairMessageTimestamp"
         FROM {quotedTableName}
         """);
   }
}
