// ============================================================
//  AcrylicTitleBarHelper.cs
//  作用：无边框亚克力弹窗的标题栏助手（静态工具类）。
//
//  背景：Avalonia 窗口一旦设置 ExtendClientAreaToDecorationsHint
//  + NoChrome，系统原生的标题栏框就没了，窗口也无法拖动。
//  为此所有弹窗在 XAML 里自绘了标题栏区域（一个 Grid 名为
//  TitleBar + 一个 Button 名为 TitleBarCloseButton），本助手
//  负责给这两个控件接上"按住拖动窗口"和"点击关闭"功能。
//
//  使用方式：在每个受影响窗口的构造函数末尾调用：
//    AcrylicTitleBarHelper.Attach(this);
//
//  适用的窗口：
//    - ComponentListWindow（组件选择窗口）
//    - EditTextWindow（编辑文本窗口）
//    - PresetSelectWindow（预设选择窗口）
//    - UsbNotificationWindow（U盘提醒窗口）
// ============================================================

using Avalonia.Controls;
using Avalonia.Input;

namespace ConvenientText.Views
{
    /// <summary>
    /// 亚克力无边框窗口标题栏助手。
    /// 为使用了 AcrylicBlur + NoChrome 的弹窗提供标题栏交互能力。
    /// </summary>
    /// <remarks>
    /// 调用前提：窗口的 XAML 中必须有：
    ///   - x:Name="TitleBar" 的 Grid 控件（作为拖动区域）
    ///   - x:Name="TitleBarCloseButton" 的 Button 控件（作为关闭按钮）
    /// 
    /// 两者缺一不可，否则会静默跳过（不会有异常）。
    /// </remarks>
    public static class AcrylicTitleBarHelper
    {
        /// <summary>
        /// 给窗口的标题栏接上交互功能。
        /// </summary>
        /// <param name="window">使用了 AcrylicBlur 无边框样式的窗口</param>
        public static void Attach(Window window)
        {
            // 查找标题栏区域（XAML 中 x:Name="TitleBar" 的 Grid）
            var titleBar = window.FindControl<Grid>("TitleBar");
            if (titleBar != null)
            {
                // 按住标题栏区域的任意位置都可以拖动窗口
                titleBar.PointerPressed += (s, e) =>
                {
                    // 只有左键才触发拖动（右键/中键不处理）
                    if (e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
                        window.BeginMoveDrag(e);
                };
            }

            // 查找关闭按钮（XAML 中 x:Name="TitleBarCloseButton" 的 Button）
            var closeButton = window.FindControl<Button>("TitleBarCloseButton");
            if (closeButton != null)
            {
                // 点击关闭按钮 = 关闭窗口
                closeButton.Click += (_, _) => window.Close();
            }
        }
    }
}
