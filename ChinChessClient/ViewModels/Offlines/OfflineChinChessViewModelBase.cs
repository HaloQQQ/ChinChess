using ChinChessCore.Contracts;
using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.ViewModels;

internal abstract class OfflineChinChessViewModelBase : ChinChessViewModelBase
{
    protected override string Name => this.IsRedTurn ? "红色" : "黑色";

    public OfflineChinChessViewModelBase(IAppConfigFileHotKeyManager appCfgHotKeyManager)
        : base(appCfgHotKeyManager)
    {
        this.InitDatas();

        this.Begin_Wav();

        this.SelectOrPutCommand = new DelegateCommand<ChinChessModel>(
            model => SelectOrPut_CommandExecute(model),
            model => this.Status == EnumGameStatus.Ready && model != null && CurrentChess != model
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CurrentChess);

        this.RevokeCommand = new DelegateCommand(
            Revoke_CommandExecute,
            () => this.Status == EnumGameStatus.Ready && CommandStack?.Count > 0
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.CommandStack.Count);
    }

    protected override bool SelectOrPut_CommandExecute(ChinChessModel model)
    {
        if (!this.SelectOrPut_CommandExecuteCore(model))
        {
            return false;
        }

        // 选中
        bool canSelect = !model.Data.IsEmpty && model.Data.IsRed == this.IsRedTurn;
        if (canSelect)
        {
            if (model.TrySelect(_preMoveVisitor))
            {
                CurrentChess = model;

                this.Select_Mp3();

                this.Log(this.Name, $"选中{model.Pos}", this.IsRedTurn == true);
            }

            return true;
        }

        return this.TryPutTo(model);
    }

    protected bool TryPutTo(ChinChessModel model)
    {
        var targetIsEmpty = model.Data.IsEmpty;

        // 移动棋子到这里 或 吃子
        if (this.CurrentChess.IsNotNullAnd(c => this.TryPutTo(c, model.Pos)))
        {
            var action = targetIsEmpty ? "移动" : "吃子";
            this.Log(this.Name, $"{action}{CurrentChess.Pos}=>{model.Pos}", this.IsRedTurn == true);

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

            if (!this.IsGameOver())
            {
                this.IsRedTurn = !IsRedTurn;
            }

            return true;
        }

        return false;
    }

    protected override void Revoke_CommandExecute()
    {
        base.Revoke_CommandExecute();

        this.Log(this.Name, "回退", this.IsRedTurn == true);
    }

    protected override void RePlay_CommandExecute()
    {
        base.RePlay_CommandExecute();

        this.Log(this.Name, "重玩", this.IsRedTurn == true);
    }

    protected override void OnGameStatusChanged(EnumGameStatus newStatus)
    {
        base.OnGameStatusChanged(newStatus);

        switch (newStatus)
        {
            case EnumGameStatus.Ready:
                this.TotalRedSeconds = this.TotalBlackSeconds = 0;
                break;
            default:
                break;
        }
    }

    #region Timer
    protected override void Timer_Tick(object sender, EventArgs e)
    {
        if (this.IsRedTurn == true)
        {
            this.TotalRedSeconds++;
        }
        else
        {
            this.TotalBlackSeconds++;
        }

        base.Timer_Tick(sender, e);
    }
    #endregion
}
