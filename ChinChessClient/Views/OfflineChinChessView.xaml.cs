using System.Windows.Controls;

#pragma warning disable CS8601 // 引用类型赋值可能为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
namespace ChinChessClient.Views;

/// <summary>
/// ChineseChessView.xaml 的交互逻辑
/// </summary>
public partial class OfflineChinChessView : UserControl
{
    public OfflineChinChessView()
    {
        InitializeComponent();
    }
}
