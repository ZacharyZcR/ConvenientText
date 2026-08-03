// ============================================================
//  UsbNotificationWindow.axaml.cs
//  作用：U盘插入时弹出的"防断头"提醒窗口交互逻辑。
//
//  功能：
//    1. 窗口打开后自动启动一个 10 秒倒计时，时间到自动关闭
//    2. 用户可手动点"确定"按钮或标题栏的 ✕ 提前关闭
//    3. 关闭时清理定时器，防止内存泄漏
//
//  设计决策：
//    - 使用 System.Timers.Timer（而非 DispatcherTimer）来倒计时，
//      因为倒计时不需要精确绑定 UI 线程；只在到期关闭时才用
//      Dispatcher.UIThread.Post 切回 UI 线程
//    - 默认 Topmost = True，确保提醒始终在最上层可见
// ============================================================

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace ConvenientText.Views
{
    /// <summary>
    /// U盘插入提醒窗口。显示 10 秒后自动关闭，也可手动关闭。
    /// </summary>
    public partial class UsbNotificationWindow : Window
    {
        /// <summary>
        /// 自动关闭定时器：10 秒后自动关闭窗口。
        /// 使用 System.Timers.Timer（后台线程触发），关闭时才切回 UI 线程。
        /// </summary>
        private readonly System.Timers.Timer? _autoCloseTimer;

        /// <summary>
        /// 构造函数。初始化界面并启动 10 秒自动关闭倒计时。
        /// </summary>
        public UsbNotificationWindow()
        {
            InitializeComponent();

            // 接上亚克力标题栏：支持拖动窗口 + 关闭按钮
            AcrylicTitleBarHelper.Attach(this);

            // 创建 10 秒倒计时定时器
            _autoCloseTimer = new System.Timers.Timer(10000);
            _autoCloseTimer.Elapsed += (s, e) =>
            {
                // 定时器回调在后台线程触发，关闭窗口必须切回 UI 线程
                Dispatcher.UIThread.Post(() =>
                {
                    this.Close();       // 关闭窗口
                });
                _autoCloseTimer.Stop(); // 停止定时器（避免重复触发）
            };
            _autoCloseTimer.Start();    // 启动倒计时
        }

        /// <summary>
        /// 用户点击"确定"按钮：停止定时器并立即关闭窗口。
        /// </summary>
        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            _autoCloseTimer?.Stop();    // 先停定时器，防止 Close() 后又触发一次
            this.Close();               // 关闭窗口
        }

        /// <summary>
        /// 窗口关闭时：确保定时器被停止和释放，防止内存泄漏。
        /// 无论窗口是自动关闭、手动关闭还是被系统关闭，这个清理都会执行。
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // 先停定时器再释放，顺序重要（先释放再停可能报 ObjectDisposedException）
            _autoCloseTimer?.Stop();
            _autoCloseTimer?.Dispose();
            base.OnClosed(e);
        }
    }
}
