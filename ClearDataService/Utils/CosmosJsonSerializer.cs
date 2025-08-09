using ClearDataService.Exceptions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Text;

namespace ClearDataService.Utils;

public sealed class CosmosJsonSerializer : CosmosSerializer
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
    private readonly JsonSerializer _serializer;

    public CosmosJsonSerializer(JsonSerializerSettings? jsonSerializerSettings = null)
    {
        _serializer = JsonSerializer.Create(jsonSerializerSettings ?? GetDefaultJsonSerializerSettings());
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            var type = typeof(T);

            if (typeof(Stream).IsAssignableFrom(type))
            {
                return (T)(object)stream;
            }

            using var sr = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(sr);

            return _serializer.Deserialize<T>(jsonReader)
                ?? throw new CosmosJsonSerializerNullException();
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var streamPayload = new MemoryStream();
        using (var streamWriter = new StreamWriter(streamPayload, encoding: DefaultEncoding, bufferSize: 1024, leaveOpen: true))
        {
            using JsonWriter writer = new JsonTextWriter(streamWriter);
            writer.Formatting = Formatting.None;
            _serializer.Serialize(writer, input);
            writer.Flush();
            streamWriter.Flush();
        }

        streamPayload.Position = 0;
        return streamPayload;
    }

    private static JsonSerializerSettings GetDefaultJsonSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = new CamelCasePrivateSettersResolver()
        };
    }

    public class CamelCasePrivateSettersResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var jsonProperty = base.CreateProperty(member, memberSerialization);

            // Enable writing to private setters
            if (!jsonProperty.Writable && member is PropertyInfo property)
            {
                jsonProperty.Writable = property.GetSetMethod(nonPublic: true) != null;
            }

            return jsonProperty;
        }
    }
}