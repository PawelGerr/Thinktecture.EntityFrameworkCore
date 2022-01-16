using System.Text.Json;
using System.Text.Json.Serialization;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Json;

public class ConvertibleClassConverter : JsonConverter<ConvertibleClass>
{
   public override ConvertibleClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      throw new NotSupportedException();
   }

   public override void Write(Utf8JsonWriter writer, ConvertibleClass value, JsonSerializerOptions options)
   {
      writer.WriteNumberValue(value.Key);
   }
}
