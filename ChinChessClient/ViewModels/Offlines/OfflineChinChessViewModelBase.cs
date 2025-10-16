using ChinChessClient.Contracts;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;

namespace ChinChessClient.ViewModels;

internal abstract class OfflineChinChessViewModelBase : ChinChessViewModelBase
{
    protected override string Name => this.IsRedTurn ? "红色" : "黑色";

    public OfflineChinChessViewModelBase(IAppConfigFileHotKeyManager appCfgHotKeyManager) : base(appCfgHotKeyManager)
    {
        this.Status = GameStatus.Ready;

        this.RevokeCommand = new DelegateCommand(
            Revoke_CommandExecute,
            () => this.Status == GameStatus.Ready && CommandStack?.Count > 0
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.CommandStack.Count);
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

    protected override void OnGameStatusChanged(GameStatus newStatus)
    {
        base.OnGameStatusChanged(newStatus);

        switch (newStatus)
        {
            case GameStatus.Ready:
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
