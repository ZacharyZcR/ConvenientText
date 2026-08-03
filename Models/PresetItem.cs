// ============================================================
//  PresetItem.cs
//  作用：预设库中的单条预设项。
//  相比旧版的纯字符串，新增了 Category 和 Name 字段，
//  支持分组显示、快速识别预设用途。
// ============================================================

using System;

namespace ConvenientText.Models
{
    /// <summary>
    /// 预设条目，支持分类分组。
    /// </summary>
    public class PresetItem : IEquatable<PresetItem>
    {
        /// <summary>预设显示名称（简短，在列表中展示）</summary>
        public string Name { get; set; } = "";

        /// <summary>预设文本内容（点击后填入输入框的实际文字）</summary>
        public string Text { get; set; } = "";

        /// <summary>所属分类。空字符串 = "未分类"</summary>
        public string Category { get; set; } = "";

        public bool Equals(PresetItem? other)
        {
            if (other is null) return false;
            return Name == other.Name && Text == other.Text && Category == other.Category;
        }

        public override bool Equals(object? obj) => Equals(obj as PresetItem);

        public override int GetHashCode() => HashCode.Combine(Name, Text, Category);
    }
}
