using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Wpf.Atom.Utils.HotKey.App;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8604 // 引用类型参数可能为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.ViewModels;

internal class OfflineChinChessViewModel : OfflineChinChessViewModelBase
{
    public override string Title => "本地版";

    public override ChinChessMode Mode => ChinChessMode.Offline;

    public OfflineChinChessViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager)
        : base(appCfgHotKeyManager)
    {
        this.Status = GameStatus.Ready;
    }

    protected override void InitDatas()
    {
        foreach (var item in this.Datas)
        {
            item.Dispose();
        }

        this.Datas.Clear();

        for (int row = 0; row < 10; row++)
        {
                for (int column = 0; column < 9; column++)
            {
                this.Datas.Add(new ChinChessModel(row, column, false));
            }
        }
    }
}
