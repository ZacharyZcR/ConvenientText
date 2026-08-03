// ============================================================
//  ConvenientTextSettingsControl.axaml.cs
//  作用："便捷文本"组件在 ClassIsland 组件设置里的面板。
//
//  设计决策：
//    便捷文本组件的所有详细设置（文字、颜色、字号、时间段等）
//    统一放在插件设置页里，组件自己的设置面板只显示一句引导提示。
//
//    原因：ClassIsland 的组件设置入口分散在各个组件上，而插件设置
//    页可以集中浏览和管理所有组件的设置。把设置收拢到一处，
//    用户体验更好，也避免了两个入口数据不同步的问题。
//
//  对应的 XAML 文件：ConvenientTextSettingsControl.axaml
// ============================================================

using Avalonia.Controls;

namespace ConvenientText.Components
{
    /// <summary>
    /// "便捷文本"组件在 ClassIsland 组件设置中的设置面板。
    /// 目前只是加载 XAML 界面（一句提示文字），不包含任何交互逻辑。
    /// </summary>
    /// <remarks>
    /// ClassIsland 要求实现 ComponentBase 的组件必须配套一个
    /// ComponentSettingsControl。这里的 InitializeComponent() 
    /// 加载的就是只含一句提示的 XAML 界面。
    /// 
    /// 如果要给本组件加独立设置，可以在这里处理业务逻辑，
    /// 但目前所有设置都在 PluginSettingsControl 中统一管理。
    /// </remarks>
    public partial class ConvenientTextSettingsControl : Avalonia.Controls.UserControl
    {
        /// <summary>
        /// 构造函数。调用 InitializeComponent() 加载 XAML 界面。
        /// 该界面只含一句引导提示，不包含任何设置控件。
        /// </summary>
        public ConvenientTextSettingsControl()
        {
            InitializeComponent();
        }
    }
}