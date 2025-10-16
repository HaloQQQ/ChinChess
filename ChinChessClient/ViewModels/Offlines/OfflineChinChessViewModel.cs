using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;

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
        this.SelectOrPutCommand = new DelegateCommand<ChinChessModel>(
            model =>
            {
                if (!this.SelectOrPutCommand_ExecuteCore(model))
                {
                    return;
                }

                var targetData = model.Data;
                var targetIsEmpty = model.Data.IsEmpty;
                // 选中
                bool canSelect = !model.Data.IsEmpty && model.Data.IsRed == this.IsRedTurn;
                if (canSelect)
                {
                    if (model.TrySelect(_preMoveVisitor))
                    {
                        CurrentChess = model;

                        this.Select_Mp3();

                        this.Log(this.Name, $"选中{model.Pos}", this.IsRedTurn);
                    }

                    return;
                }

                // 移动棋子到这里 或 吃子
                if (this.CurrentChess.IsNotNullAnd(c => this.TryPutTo(c, model.Pos)))
                {
                    var action = targetIsEmpty ? "移动" : "吃子";
                    this.Log(this.Name, $"{action}{CurrentChess.Pos}=>{model.Pos}", this.IsRedTurn);

                    if (targetIsEmpty)
                    {
                        this.Go_Mp3();
                    }
                    else
                    {
                        this.Eat_Mp3();
                    }

                    this.From = this.CurrentChess;
                    this.To = model;

                    this.CurrentChess = null;

                    if (!this.CheckGameOver())
                    {
                        this.IsRedTurn = !IsRedTurn;
                    }
                }
            },
            model => this.Status == GameStatus.Ready && model != null && model != CurrentChess
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CurrentChess);
    }

    protected override void InitDatas()
    {
        foreach (var item in this.Datas)
        {
            item.Dispose();
        }

        WpfAtomUtils.BeginInvoke(() =>
        {
            this.Datas.Clear();

            for (int row = 0; row < 10; row++)
            {
                for (int column = 0; column < 9; column++)
                {
                    this.Datas.Add(new ChinChessModel(row, column, false));
                }
            }
        });
    }
}
