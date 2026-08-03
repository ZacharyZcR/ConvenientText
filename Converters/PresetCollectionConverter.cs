// ============================================================
//  PresetCollectionConverter.cs
//  作用：预设集合的 JSON 转换器。
//  兼容两种格式：
//    旧版纯字符串：["文本1", "文本2"]
//    新版对象：    [{"Name":"xx","Text":"xx","Category":"xx"}]
//  通过 [JsonConverter] 特性挂在 Presets 属性上，
//  确保 ClassIsland 自己的序列化器也能正确读写。
// ============================================================

using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConvenientText.Models;

namespace ConvenientText.Converters
{
    public class PresetCollectionConverter : JsonConverter<ObservableCollection<PresetItem>>
    {
        public override ObservableCollection<PresetItem> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new ObservableCollection<PresetItem>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            using var doc = JsonDocument.ParseValue(ref reader);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    // 旧版格式：纯字符串预设，自动迁移为 PresetItem
                    var text = element.GetString() ?? "";
                    list.Add(new PresetItem { Name = text, Text = text, Category = "" });
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    // 新版格式：直接反序列化对象
                    var item = JsonSerializer.Deserialize<PresetItem>(element.GetRawText(), options);
                    if (item != null) list.Add(item);
                }
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, ObservableCollection<PresetItem> value, JsonSerializerOptions options)
        {
            // 手动写出数组，避免递归调用 options 中的同类转换器
            writer.WriteStartArray();
            foreach (var item in value)
            {
                writer.WriteStartObject();
                if (!string.IsNullOrEmpty(item.Name))
                    writer.WriteString("Name", item.Name);
                if (!string.IsNullOrEmpty(item.Text))
                    writer.WriteString("Text", item.Text);
                if (!string.IsNullOrEmpty(item.Category))
                    writer.WriteString("Category", item.Category);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
