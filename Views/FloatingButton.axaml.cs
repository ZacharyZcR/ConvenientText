// ============================================================
//  FloatingButton.axaml.cs
//  作用：桌面上的悬浮编辑按钮（一个无边框小圆钮窗口）。
//  特性：
//    · 置底显示（通过 Win32 SetWindowPos 放到窗口层最底部）；
//    · 可拖动，松手后把新位置保存到共享存储；
//    · 点击打开“选择组件”窗口；
//    · 是否显示由 FloatingWindowHostedService（Plugin.cs 里）
//      根据开关、时间段、组件是否加载来统一控制。
//  本文件也是代码建界面，没有对应的 .axaml 文件。
// ============================================================

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using ConvenientText.Models;
using ConvenientText.Services;

// 消除歧义别名
using AvaloniaBrushes = Avalonia.Media.Brushes;
using AvaloniaButton = Avalonia.Controls.Button;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaCursor = Avalonia.Input.Cursor;

namespace ConvenientText.Views
{
    /// <summary>
    /// 桌面悬浮编辑按钮。一个 56x56 的无边框透明小窗口，
    /// 里面放一个圆形 ✎ 按钮；支持拖动换位，点击打开组件列表。
    /// </summary>
    public partial class FloatingButton : Window
    {
        /// <summary>当前绑定的组件数据（决定按钮初始位置；拖动后坐标也写回它）</summary>
        private TextDataModel _dataModel;

        /// <summary>共享数据存储，拖动结束时保存新位置</summary>
        private readonly DataStorageService _storage;

        /// <summary>当前打开的组件列表窗口（防重复打开）</summary>
        private static ComponentListWindow? _openListWindow;
        private static readonly object _listWindowLock = new();

        /// <summary>悬浮按钮本体元素（圈内 ✎ 按钮），用于动态换色</summary>
        private readonly AvaloniaButton _button;

        /// <summary>窗口句柄（Win32 置底操作用，仅 Windows 有效）</summary>
        private IntPtr _hwnd = IntPtr.Zero;

        /// <summary>防止 Loaded 事件重复初始化</summary>
        private bool _isLoaded = false;

        // ----- 拖动状态机 -----
        private bool _isPointerDown = false;    // 左键是否按着
        private bool _isDragging = false;       // 是否已经进入拖动（超过阈值）
        private PixelPoint _windowPosOnDown;    // 按下瞬间的窗口位置
        private PixelPoint _mouseScreenOnDown;  // 按下瞬间的鼠标屏幕坐标

        /// <summary>拖动阈值（像素）：按下后移动超过它才算拖动，否则算点击。
        /// 触屏上手指比鼠标精度差，稍调高阈值减少误触。</summary>
        private const double DragThreshold = 14;

        public FloatingButton(TextDataModel dataModel, DataStorageService storage)
        {
            _dataModel = dataModel;
            _storage = storage;

            // ----- 窗口基本形态：外壳比按钮大一圈，方便拖动 -----
            // 【触控优化】从 50×50 增大到 60×60，符合触屏最小 48px 推荐标准
            Width = 60;
            Height = 60;
            CanResize = false;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Topmost = false;
            Title = "";

            // ----- 无边框 + 亚克力模糊：按钮背后有磨砂玻璃效果 -----
            SystemDecorations = SystemDecorations.None;
            TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur };
            Background = AvaloniaBrushes.Transparent;
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            ExtendClientAreaTitleBarHeightHint = 0;

            Position = new PixelPoint((int)_dataModel.FloatingX, (int)_dataModel.FloatingY);

            this.Loaded += OnLoaded!;
            this.Deactivated += OnDeactivated!;

            // 【触控优化】确保触屏拖动手势能被正确识别
            // （PointerPressed/PointerMoved/PointerReleased 在 Avalonia 中是统一的指针事件，已覆盖触屏）

            // ----- 新视觉：圆角小方标 + 铅笔图标 -----
            // 不是圆点，是一个带圆角的小方块，像桌面上的迷你编辑标签
            var button = new AvaloniaButton
            {
                Content = "✎",
                FontSize = 15,
                Background = new SolidColorBrush(AvaloniaColor.FromArgb(220, 68, 68, 68)),
                Foreground = AvaloniaBrushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Width = 30,
                Height = 30,
                Padding = new Thickness(0),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Cursor = new AvaloniaCursor(StandardCursorType.Hand)
            };
            _button = button;

            // 亚克力磨砂底色：比按钮大一圈的半透明圆角方块，产生模糊效果
            var backdrop = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(AvaloniaColor.FromArgb(40, 255, 255, 255)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var grid = new Grid();
            grid.Children.Add(backdrop);
            grid.Children.Add(button);
            button.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            button.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            Content = grid;

            button.Click += OnButtonClick;

            this.PointerPressed += OnPointerPressed!;
            this.PointerMoved += OnPointerMoved!;
            this.PointerReleased += OnPointerReleased!;
        }

        public void UpdateDataModel(TextDataModel newModel)
        {
            bool sameComponent = _dataModel.ComponentId == newModel.ComponentId;
            _dataModel = newModel;
            if (!sameComponent)
            {
                Position = new PixelPoint((int)_dataModel.FloatingX, (int)_dataModel.FloatingY);
                ClampPositionToScreen();
            }
        }

        /// <summary>确保按钮位置不跑到屏幕外面去</summary>
        private void ClampPositionToScreen()
        {
            try
            {
                var screens = this.Screens;
                if (screens == null || screens.ScreenCount == 0) return;

                double x = Position.X, y = Position.Y;
                double w = Width, h = Height;

                bool onAnyScreen = false;
                for (int i = 0; i < screens.ScreenCount; i++)
                {
                    var bounds = screens.All[i].Bounds;
                    if (x + w > bounds.X && x < bounds.X + bounds.Width &&
                        y + h > bounds.Y && y < bounds.Y + bounds.Height)
                    {
                        onAnyScreen = true;
                        if (x < bounds.X) x = bounds.X + 10;
                        if (y < bounds.Y) y = bounds.Y + 10;
                        if (x + w > bounds.X + bounds.Width) x = bounds.X + bounds.Width - w - 10;
                        if (y + h > bounds.Y + bounds.Height) y = bounds.Y + bounds.Height - h - 10;
                        break;
                    }
                }

                if (!onAnyScreen && screens.ScreenCount > 0)
                {
                    var primary = screens.All[0].Bounds;
                    x = primary.X + 20;
                    y = primary.Y + 100;
                }

                Position = new PixelPoint((int)x, (int)y);
            }
            catch { }
        }

        private void OnButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                lock (_listWindowLock)
                {
                    // 已有窗口开着 → 激活它，不新建
                    if (_openListWindow != null && _openListWindow.IsVisible)
                    {
                        _openListWindow.Activate();
                        return;
                    }

                    var listWindow = new ComponentListWindow();
                    listWindow.Closed += (_, _) =>
                    {
                        lock (_listWindowLock) { _openListWindow = null; }
                    };
                    _openListWindow = listWindow;
                    listWindow.Show();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConvenientText] Failed to open ComponentListWindow: {ex.Message}");
            }
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_isLoaded) return;
            _isLoaded = true;

            // 位置校验：防止上次拖到屏幕外
            ClampPositionToScreen();

            if (!OperatingSystem.IsWindows()) return;
            var handle = this.TryGetPlatformHandle()?.Handle;
            if (handle == null || handle.Value == IntPtr.Zero) return;
            _hwnd = handle.Value;

            try
            {
                // 1) 禁用 DWM 非客户区渲染
                int policy = 2;
                DwmSetWindowAttribute(_hwnd, DWMWA_NCRENDERING_POLICY, ref policy, sizeof(int));

                // 2) 从 Alt+Tab 和任务栏隐藏
                IntPtr exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE);
                long style = exStyle.ToInt64();
                style |= (WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
                style &= ~WS_EX_APPWINDOW;
                SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(style));

                // 3) 从 Win+Tab 多任务视图隐藏：设置 excluded from peek
                int excluded = 1;
                DwmSetWindowAttribute(_hwnd, DWMWA_EXCLUDED_FROM_PEEK, ref excluded, sizeof(int));

                // 4) 置底
                SetWindowPos(_hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            catch { }
        }

        private void OnDeactivated(object? sender, EventArgs e)
        {
            if (_hwnd != IntPtr.Zero)
            {
                try
                {
                    SetWindowPos(_hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    // 重新应用 peek 排除（可能被某些系统事件重置）
                    int excluded = 1;
                    DwmSetWindowAttribute(_hwnd, DWMWA_EXCLUDED_FROM_PEEK, ref excluded, sizeof(int));
                }
                catch { }
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isPointerDown = true;
                _isDragging = false;
                _windowPosOnDown = this.Position;
                _mouseScreenOnDown = this.PointToScreen(e.GetPosition(this));
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPointerDown) return;

            var mouseScreenCurrent = this.PointToScreen(e.GetPosition(this));
            var delta = mouseScreenCurrent - _mouseScreenOnDown;

            if (Math.Abs(delta.X) > DragThreshold || Math.Abs(delta.Y) > DragThreshold)
            {
                if (!_isDragging) _isDragging = true;

                var newX = _windowPosOnDown.X + delta.X;
                var newY = _windowPosOnDown.Y + delta.Y;
                this.Position = new PixelPoint(newX, newY);
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isPointerDown) return;
            _isPointerDown = false;

            if (_isDragging)
            {
                var pos = this.Position;

                // 【修复】旧版先把新坐标写进内存对象，却又从磁盘重新读了一份
                // 旧数据再保存，导致坐标永远存不上。现在先读出字典、更新
                // 对应组件的坐标、再整体保存。
                try
                {
                    var all = _storage.LoadAll();
                    if (all.TryGetValue(_dataModel.ComponentId, out var stored))
                    {
                        stored.FloatingX = pos.X;
                        stored.FloatingY = pos.Y;
                    }
                    else
                    {
                        _dataModel.FloatingX = pos.X;
                        _dataModel.FloatingY = pos.Y;
                        all[_dataModel.ComponentId] = _dataModel.Clone();
                    }
                    _storage.SaveAll(all);
                }
                catch { }

                if (_hwnd != IntPtr.Zero)
                {
                    try
                    {
                        SetWindowPos(_hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    }
                    catch { }
                }
                _isDragging = false;
            }
        }

        // ============================================================
        //  Win32 P/Invoke
        // ============================================================
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_NCRENDERING_POLICY = 2;
        private const int DWMWA_EXCLUDED_FROM_PEEK = 12;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_APPWINDOW = 0x00040000;
    }
}
