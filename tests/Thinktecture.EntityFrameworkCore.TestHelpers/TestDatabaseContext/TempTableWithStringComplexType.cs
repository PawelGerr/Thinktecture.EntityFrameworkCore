namespace Thinktecture.TestDatabaseContext;

/// <summary>
/// Temp-table-only entity with string members inside a complex type; tests register it via <c>ConfigureModel</c>.
/// </summary>
public class TempTableWithStringComplexType
{
   public Guid Id { get; set; }
   public TextPair Texts { get; set; }

   public static void Configure(ModelBuilder modelBuilder, string collation)
   {
      modelBuilder.ConfigureTempTableEntity<TempTableWithStringComplexType>(builder => builder.ComplexProperty(e => e.Texts, c => c.Property(t => t.WithCollation).UseCollation(collation)));
   }
}
