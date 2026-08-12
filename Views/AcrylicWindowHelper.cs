// ============================================================
//  AcrylicWindowHelper.cs
//  作用：亚克力效果的全局开关应用器。
//
//  【1.2.2.1 新增】部分环境下亚克力模糊背景可读性不佳
//  （浅色桌面、低对比度壁纸等），设置页新增"窗口亚克力效果"
//  全局开关。本助手负责在窗口创建时读取该开关：
//    · 开启（默认）：保持 AcrylicBlur 亚克力效果，什么都不做；
//    · 关闭：窗口改为不透明背景（跟随系统深浅色主题），
//      文字与控件对比度更高，可读性更好。
//
//  使用方式：在每个受影响窗口的构造函数末尾调用：
//    AcrylicWindowHelper.Apply(this);
//
//  适用的窗口：
//    - ComponentListWindow（组件选择窗口）
//    - EditTextWindow（编辑文本窗口）
//    - PresetSelectWindow（预设选择窗口）
//    - UsbNotificationWindow（U盘提醒窗口）
//    （FloatingButton 没有根 Border，自行在代码里实心化底色）
//
//  【1.2.2.1】另提供提示弹窗工厂 CreateMessageWindow()，
//  把代码动态创建的提示框统一成亚克力无边框样式并跟随亚克力开关。
// ============================================================

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace ConvenientText.Views
{
    /// <summary>
    /// 亚克力效果全局开关应用器。
    /// 根据设置页"窗口亚克力效果"开关调整弹窗的外观。
    /// </summary>
    public static class AcrylicWindowHelper
    {
        /// <summary>
        /// 根据全局"窗口亚克力效果"开关调整窗口外观。
        /// 开关开启（默认）时什么都不做；关闭时把窗口改为不透明背景。
        /// 注意：只对【新打开】的窗口生效，已打开的窗口不会变化。
        /// </summary>
        /// <param name="window">亚克力无边框弹窗</param>
        public static void Apply(Window window)
        {
            var storage = Plugin.Storage;
            if (storage == null || storage.GlobalUseAcrylic) return;

            // 1) 取消亚克力透明级别：窗口变成普通不透明窗口
            window.TransparencyLevelHint = new[] { WindowTransparencyLevel.None };

            // 2) 窗口背景换成不透明主题色（避免原来 Transparent 背景露黑）
            var opaque = GetOpaqueBackground(window);
            window.Background = opaque;

            // 3) 根 Border（磨砂玻璃容器）也换成不透明背景，
            //    否则半透明的 SystemControlBackgroundChromeMediumLowBrush
            //    在没有模糊的情况下会把桌面内容透出来，反而更乱。
            if (window.Content is Border border)
                border.Background = opaque;
        }

        /// <summary>
        /// 取一个不透明的主题背景色（跟随系统深浅色）。
        /// 优先用 Fluent 主题自带的 SystemControlBackgroundAltHighBrush
        /// （深浅色自动切换），取不到再按窗口主题手动兜底。
        /// </summary>
        private static IBrush GetOpaqueBackground(Window window)
        {
            return TryGetThemeBrush(window, "SystemControlBackgroundAltHighBrush")
                   ?? new SolidColorBrush(Color.Parse(
                       window.ActualThemeVariant == ThemeVariant.Dark ? "#FF202020" : "#FFF3F3F3"));
        }

        // ============================================================
        //  提示弹窗工厂（代码创建的提示框统一入口）
        //  【1.2.2.1】原先零散的 new Window 提示框是系统默认样式，
        //  与插件的亚克力无边框风格不统一，也无法跟随亚克力开关。
        //  现在统一走这个方法：亚克力无边框 + 可拖动标题栏 + 关闭按钮，
        //  并且自动跟随"窗口亚克力效果"开关。
        // ============================================================

        /// <summary>
        /// 创建一个"提示"弹窗（亚克力无边框样式，跟随全局亚克力开关）。
        /// 调用方自行决定 Show() 或 ShowDialog(owner)。
        /// </summary>
        /// <param name="title">标题文字</param>
        /// <param name="message">正文内容（自动换行）</param>
        /// <param name="buttonText">底部按钮文字，默认"确定"</param>
        public static Window CreateMessageWindow(string title, string message, string buttonText = "确定")
        {
            var window = new Window
            {
                Title = title,
                Width = 380,
                Height = 200,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ExtendClientAreaToDecorationsHint = true,
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
                ExtendClientAreaTitleBarHeightHint = -1,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur },
                Background = Brushes.Transparent
            };

            // ----- 标题栏：拖动区域 + 关闭按钮 -----
            var titleText = new TextBlock
            {
                Text = title,
                FontWeight = FontWeight.SemiBold,
                FontSize = 14,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var closeButton = new Button
            {
                Content = "✕",
                FontSize = 13,
                Width = 30,
                Height = 30,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brush.Parse("#CCFFFFFF"),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            closeButton.Click += (_, _) => window.Close();
            closeButton.PointerEntered += (_, _) =>
            {
                closeButton.Background = Brush.Parse("#E81123");
                closeButton.Foreground = Brushes.White;
            };
            closeButton.PointerExited += (_, _) =>
            {
                closeButton.Background = Brushes.Transparent;
                closeButton.Foreground = Brush.Parse("#CCFFFFFF");
            };

            var titleBar = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetColumn(titleText, 0);
            Grid.SetColumn(closeButton, 1);
            titleBar.Children.Add(titleText);
            titleBar.Children.Add(closeButton);
            titleBar.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
                    window.BeginMoveDrag(e);
            };

            // ----- 正文 + 按钮 -----
            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var okButton = new Button
            {
                Content = buttonText,
                Width = 80,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Classes = { "Accent" }
            };
            okButton.Click += (_, _) => window.Close();

            var layout = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto")
            };
            Grid.SetRow(titleBar, 0);
            Grid.SetRow(messageText, 1);
            Grid.SetRow(okButton, 2);
            layout.Children.Add(titleBar);
            layout.Children.Add(messageText);
            layout.Children.Add(okButton);

            // ----- 磨砂玻璃容器 -----
            window.Content = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 10, 16, 16),
                Background = TryGetThemeBrush(window, "SystemControlBackgroundChromeMediumLowBrush")
                             ?? new SolidColorBrush(Color.FromArgb(200, 32, 32, 32)),
                Child = layout
            };

            // 跟随全局亚克力开关（关闭时改为不透明背景）
            Apply(window);
            return window;
        }

        /// <summary>尝试从主题资源里取画刷，取不到返回 null</summary>
        private static IBrush? TryGetThemeBrush(Window window, string key)
        {
            try
            {
                if (window.TryFindResource(key, out var brush) && brush is IBrush b)
                    return b;
            }
            catch { }
            return null;
        }
    }
}
