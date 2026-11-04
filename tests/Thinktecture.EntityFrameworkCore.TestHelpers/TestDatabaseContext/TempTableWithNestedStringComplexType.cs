namespace Thinktecture.TestDatabaseContext;

/// <summary>
/// Temp-table-only entity with a string member two complex levels deep; tests register it via <c>ConfigureModel</c>.
/// </summary>
public class TempTableWithNestedStringComplexType
{
   public Guid Id { get; set; }
   public TextEnvelope Envelope { get; set; }

   public static void Configure(ModelBuilder modelBuilder, string collation)
   {
      modelBuilder.ConfigureTempTableEntity<TempTableWithNestedStringComplexType>(builder => builder.ComplexProperty(e => e.Envelope, envelope => envelope.ComplexProperty(t => t.Texts, texts => texts.Property(t => t.WithCollation).UseCollation(collation))));
   }
}
