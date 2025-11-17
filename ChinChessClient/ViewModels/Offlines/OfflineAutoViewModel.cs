using ChinChessClient.AutomationEngines;
using ChinChessClient.Commands;
using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;

namespace ChinChessClient.ViewModels;
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。

internal class OfflineAutoViewModel : OfflineChinChessViewModel
{
    private IEleEyeEngine _eleEyeEngine;

    public override ChinChessMode Mode => ChinChessMode.OfflineAuto;

    public OfflineAutoViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager, IEleEyeEngine eleEyeEngine)
        : base(appCfgHotKeyManager)
    {
        this.Status = GameStatus.Ready;
        this._eleEyeEngine = eleEyeEngine;

        this.InitAsync();

        _eleEyeEngine.OnBestMoveReceived += EleEyeEngine_OnBestMoveReceived;

        this.SelectOrPutCommand = new DelegateCommand<ChinChessModel>(
            SelectOrPut_CommandExecute,
            model => this.Status == GameStatus.Ready 
                        && model != null && CurrentChess != model
                        && IsRedTurn
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CurrentChess);

        this.RevokeCommand = new DelegateCommand(
            Revoke_CommandExecute,
            () => this.Status == GameStatus.Ready 
                    && CommandStack?.Count > 0
                    && IsRedTurn
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CommandStack.Count);
    }

    private void EleEyeEngine_OnBestMoveReceived(MovePath movePath)
    {
        var from = this.Datas[movePath.From.Index];

        if (from.TrySelect(_preMoveVisitor))
        {
            CurrentChess = from;

            var to = this.Datas[movePath.To.Index];
            this.TryPutTo(to);
        }
    }

    private async Task InitAsync()
    {
        try
        {
            var isStarted = await _eleEyeEngine.StartAsync();

            this.PublishMsg("象棋引擎已启动");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"象棋引擎启动失败:{ex.Message}");
        }
    }

    protected override void RePlay_CommandExecute()
    {
        base.RePlay_CommandExecute();

        _eleEyeEngine.InitData(string.Empty);
    }

    protected override void SelectOrPut_CommandExecute(ChinChessModel model)
    {
        bool isRedTurn = this.IsRedTurn;
        base.SelectOrPut_CommandExecute(model);

        if (isRedTurn != this.IsRedTurn)
        {
            _eleEyeEngine.Move(this.CommandStack.As<IReadOnlyList<MoveCommand>>(), 3000);
        }
    }

    protected override void DisposeCore()
    {
        base.DisposeCore();

        _eleEyeEngine.OnBestMoveReceived -= EleEyeEngine_OnBestMoveReceived;

        _eleEyeEngine.Dispose();
        _eleEyeEngine = null;
    }
}
