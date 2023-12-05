using System.Text.Json;
using System.Text.Json.Serialization;
using Aer.QdrantClient.Http.Exceptions;
using Aer.QdrantClient.Http.Models.Primitives;

namespace Aer.QdrantClient.Http.Infrastructure.Json.Converters;

internal class SearchGroupIdJsonConverter : JsonConverter<SearchGroupId>
{
    public override SearchGroupId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
            {
                var groupId = reader.GetString();

                return SearchGroupId.String(groupId);
            }
            case JsonTokenType.Number:
                return SearchGroupId.Integer(reader.GetInt64());
            default:
                throw new QdrantJsonValueParsingException(reader.GetString());
        }
    }

    public override void Write(Utf8JsonWriter writer, SearchGroupId value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.ToJson(), JsonSerializerConstants.SerializerOptions);
    }
}
