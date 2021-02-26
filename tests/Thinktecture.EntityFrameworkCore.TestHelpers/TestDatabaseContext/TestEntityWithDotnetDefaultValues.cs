using System;

namespace Thinktecture.TestDatabaseContext
{
   public class TestEntityWithDotnetDefaultValues
   {
      public Guid Id { get; set; }
      public int Int { get; set; }
      public int? NullableInt { get; set; }
#pragma warning disable 8618
      public string String { get; set; }
#pragma warning restore 8618
      public string? NullableString { get; set; }
   }
}
