namespace GoDaddyPseudoStatic
{
    using GoDaddyPseudoStatic.RunSchedule;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class InheritanceConverter<T> : JsonConverter<T>
    {
        public readonly IReadOnlyDictionary<string, Type> _types;

        public InheritanceConverter(IReadOnlyDictionary<string, Type> types)
        {
            _types = types;
        }

        public InheritanceConverter() : this(AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => typeof(T).IsAssignableFrom(x)).ToDictionary(x => x.Name, x => x)) { }

        public override T Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (typeof(T) != typeToConvert) throw new ArgumentException($"Can't convert objects of type {typeToConvert}", nameof(typeToConvert));

            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();

            string propertyName = reader.GetString();

            if (!_types.TryGetValue(propertyName, out var type)) throw new JsonException();

            var result = (T)JsonSerializer.Deserialize(ref reader, type, options);

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject) throw new JsonException();

            return result;
        }

        public override void Write(
            Utf8JsonWriter writer,
            T value,
            JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, (IRunSchedule)null, options);
                    break;
                default:
                    {
                        writer.WriteStartObject();
                        var type = value.GetType();
                        writer.WritePropertyName(type.Name);
                        JsonSerializer.Serialize(writer, value, type, options);
                        writer.WriteEndObject();
                        break;
                    }
            }
        }
    }
}
