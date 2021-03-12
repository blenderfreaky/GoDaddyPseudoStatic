namespace GoDaddyPseudoStatic
{
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Buffers;
    using System.Linq;
    using System.Text.Json;

    public static class ConfigurationSectionExtensions
    {
        public static ReadOnlySpan<byte> ToJson(this IConfigurationSection section)
        {
            ArrayBufferWriter<byte> bufferWriter = new();
            using var writer = new Utf8JsonWriter(bufferWriter);
            section.ToJson(writer);
            writer.Flush();
            return bufferWriter.WrittenSpan;
        }

        public static void ToJson(this IConfigurationSection section, Utf8JsonWriter writer)
        {
            if (section.Value != null)
            {
                writer.WriteStringValue(section.Value);
                return;
            }

            var children = section.GetChildren().ToList();

            if (children.Select((x, i) => int.TryParse(x.Key, out int num) && num == i).All(x => x))
            {
                writer.WriteStartArray();

                foreach (var child in children) child.ToJson(writer);

                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStartObject();

                foreach (var child in children)
                {
                    writer.WritePropertyName(child.Key);
                    child.ToJson(writer);
                }

                writer.WriteEndObject();
            }
        }
    }
}