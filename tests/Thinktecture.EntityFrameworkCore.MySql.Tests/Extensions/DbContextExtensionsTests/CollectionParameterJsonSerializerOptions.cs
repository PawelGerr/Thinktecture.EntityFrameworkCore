using System.Text.Json;
using Thinktecture.Json;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// EF Core creates a service provider per distinct set of options and compares JsonSerializerOptions by reference.
// The collection parameter tests therefore share these instances instead of creating their own, otherwise the test run
// exceeds the limit of 20 EF service providers (ManyServiceProvidersCreatedWarning is an error by default).
internal static class CollectionParameterJsonSerializerOptions
{
   public static readonly JsonSerializerOptions Default = Create(null);
   public static readonly JsonSerializerOptions CamelCase = Create(JsonNamingPolicy.CamelCase);
   public static readonly JsonSerializerOptions SnakeCaseLower = Create(JsonNamingPolicy.SnakeCaseLower);

   private static JsonSerializerOptions Create(JsonNamingPolicy? namingPolicy)
   {
      return new JsonSerializerOptions { Converters = { new ConvertibleClassConverter() }, PropertyNamingPolicy = namingPolicy };
   }
}
