// ============================================================
//  ComponentSettingsPanel.axaml.cs
//  作用：设置页里"组件详细设置"卡片的内容面板。
//
//  这个控件本身几乎不做业务逻辑，所有的设置项都通过 XAML
//  的双向绑定（TwoWay Binding）直接读写 TextDataModel，修改
//  实时触发 PropertyChanged → PluginSettingsControl 的自动保存。
//
//  这里只负责两件事：
//    1. 构造时初始化一个安全的默认 DataContext（避免绑定警告）
//    2. 提供 SetDataModel() 让外部把一个组件模型"挂"进来
// ============================================================

using Avalonia.Controls;
using ConvenientText.Models;

using UserControl = Avalonia.Controls.UserControl;

namespace ConvenientText.Views
{
    /// <summary>
    /// 组件详细设置面板。
    /// 本身是纯 XAML 控件，所有输入控件都双向绑定到 TextDataModel。
    /// </summary>
    /// <remarks>
    /// 数据流：
    /// 用户操作 → XAML 双向绑定 → TextDataModel.PropertyChanged
    /// → PluginSettingsControl.OnDetailModelPropertyChanged
    /// → 写入共享存储 → 广播 DataChanged → 其他窗口同步
    /// </remarks>
    public partial class ComponentSettingsPanel : UserControl
    {
        /// <summary>
        /// 构造函数。
        /// 给一个安全的默认 DataContext，防止在还没选中组件时
        /// XAML 绑定落到外层设置窗口上，产生大量绑定警告。
        /// </summary>
        public ComponentSettingsPanel()
        {
            InitializeComponent();
            // 默认创建一个空模型作为 DataContext，所有属性都是默认值，
            // 避免 XAML 绑定时找不到数据源
            this.DataContext = new TextDataModel();
        }

        /// <summary>
        /// 把指定组件的模型"挂"到本面板上，作为 DataContext。
        /// XAML 中的双向绑定会自动读取/写入该模型的属性。
        /// </summary>
        /// <param name="model">要编辑的组件数据模型</param>
        public void SetDataModel(TextDataModel model)
        {
            this.DataContext = model;
        }
    }
}
