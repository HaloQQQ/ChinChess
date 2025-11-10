using ChinChessClient.Models;
using ChinChessClient.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChinChessClient.Views;
#pragma warning disable CS8604 // 引用类型参数可能为 null。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。

/// <summary>
/// OfflineCustomView.xaml 的交互逻辑
/// </summary>
public partial class OfflineCustomView : UserControl
{
    public OfflineCustomView()
    {
        InitializeComponent();
    }

    private void CustomChess_DragLeave(object sender, MouseEventArgs e)
    {
        e.Handled = true;

        if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
        {
            var item = (ListBoxItem)sender;
            DragDrop.DoDragDrop(item, item.DataContext, DragDropEffects.Copy);
        }
    }

    private void Chess_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (e.Data.GetDataPresent(typeof(CustomChess)))
        {
            if (sender is ListBoxItem listBoxItem && listBoxItem.DataContext is ChinChessModel chessModel && chessModel.Data.IsEmpty)
            {
                if (this.DataContext is OfflineCustomViewModel viewModel)
                {
                    var chess = e.Data.GetData(typeof(CustomChess)) as CustomChess;
                    viewModel.SetData(chessModel.Pos, chess);
                }
            }
        }
    }

    private void ChessRemove_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (sender is ListBoxItem listBoxItem && listBoxItem.DataContext is ChinChessModel chessModel && !chessModel.Data.IsEmpty)
        {
            if (this.DataContext is OfflineCustomViewModel viewModel)
            {
                viewModel.ReturnData(chessModel);
            }
        }
    }
}
