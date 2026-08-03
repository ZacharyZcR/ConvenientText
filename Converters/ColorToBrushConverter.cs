// ============================================================
//  ColorToBrushConverter.cs
//  作用：XAML 绑定用的颜色→画刷转换器（IValueConverter）。
//
//  为什么需要它：
//    Avalonia 的 Foreground/Background 等属性类型是 IBrush（画刷），
//    而 TextDataModel 中存储的是 Color（颜色值）。
//    如果直接把 Color 绑定到 Foreground，Avalonia 会报警告
//    "Could not convert ... to IBrush" 且颜色不显示。
//    通过这个转换器把 Color 包一层 SolidColorBrush，绑定就能正常工作。
//
//  用法示例（XAML）：
//    Foreground="{Binding DotColor,
//        Converter={x:Static conv:ColorToBrushConverter.Instance}}"
//
//  使用位置：
//    - PluginSettingsControl.axaml：组件列表的圆点颜色
//    - ComponentListWindow.axaml：组件选择列表的圆点颜色
//    - ComponentSettingsPanel.axaml：组件设置标题和预览区的颜色
//
//  注意：暴露为静态单例 Instance，XAML 中直接通过 x:Static 引用，
//  不需要在每个绑定处都 new 一个实例。
// ============================================================

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ConvenientText.Converters
{
    /// <summary>
    /// 把 Avalonia Color 转换为画刷（SolidColorBrush）。
    /// 双向转换器：Color → Brush（正向）和 Brush → Color（反向）。
    /// </summary>
    /// <remarks>
    /// 单例模式：整个应用共享一个 Instance，通过 XAML 的 x:Static 引用。
    /// 不需要在每个控件绑定处分别创建实例，节省内存。
    /// </remarks>
    public class ColorToBrushConverter : IValueConverter
    {
        /// <summary>全局单例，XAML 中通过 x:Static 引用</summary>
        public static readonly ColorToBrushConverter Instance = new();

        /// <summary>
        /// 正向转换：Color → SolidColorBrush。
        /// XAML 绑定时值从数据源流向控件，调用此方法。
        /// </summary>
        /// <param name="value">数据源的 Color 值</param>
        /// <param name="targetType">目标类型（通常是 IBrush）</param>
        /// <param name="parameter">未使用</param>
        /// <param name="culture">未使用</param>
        /// <returns>SolidColorBrush 实例，或灰色兜底</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
                return new SolidColorBrush(color);  // 把颜色包装成画刷
            return Brushes.Gray;                     // 未知类型：兜底灰色
        }

        /// <summary>
        /// 反向转换：SolidColorBrush → Color。
        /// 当绑定时模式为 TwoWay，控件值变化要写回数据源时调用。
        /// 例如：ColorPicker 选择的颜色需要写回 TextDataModel.TextColor。
        /// </summary>
        /// <param name="value">控件的 SolidColorBrush 值</param>
        /// <param name="targetType">目标类型（通常是 Color）</param>
        /// <param name="parameter">未使用</param>
        /// <param name="culture">未使用</param>
        /// <returns>Color 值，或白色兜底</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
                return brush.Color;    // 从画刷中提取颜色值
            return Colors.White;       // 未知类型：兜底白色
        }
    }
}
