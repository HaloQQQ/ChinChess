using ChinChessClient.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

#pragma warning disable CS8601 // 引用类型赋值可能为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
namespace ChinChessClient.Views;

/// <summary>
/// ChineseChessView.xaml 的交互逻辑
/// </summary>
public partial class OnlineChinChessView : UserControl
{
    private ChinChessViewModelBase _viewModel;

    public OnlineChinChessView()
    {
        InitializeComponent();

        _viewModel = this.DataContext as ChinChessViewModelBase;
    }

    #region 消息框
    private void Border_MouseLeave(object sender, MouseEventArgs e)
    {
        e.Handled = true;

        if (_viewModel.DialogMessage != null)
        {
            _viewModel.DialogMessage.IsEnable = true;
        }
    }

    private void Border_MouseEnter(object sender, MouseEventArgs e)
    {
        e.Handled = true;

        if (_viewModel.DialogMessage != null)
        {
            _viewModel.DialogMessage.IsEnable = false;
        }
    }
    #endregion
}
