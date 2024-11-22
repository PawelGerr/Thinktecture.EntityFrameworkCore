namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// DB schema.
/// </summary>
public sealed class DbDefaultSchema : IDbDefaultSchema
{
   /// <summary>
   /// Database schema
   /// </summary>
   public string Schema { get; }

   /// <summary>
   /// Initializes new instance of <see cref="DbDefaultSchema"/>.
   /// </summary>
   /// <param name="schema"></param>
   public DbDefaultSchema(string schema)
   {
      Schema = schema ?? throw new ArgumentNullException(nameof(schema));
   }
}
