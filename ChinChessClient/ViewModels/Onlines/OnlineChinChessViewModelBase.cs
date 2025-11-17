using ChinChessClient.Contracts;
using ChinChessCore.Models;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Atom.Extensions;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Microsoft.AspNetCore.SignalR.Client;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using ChinChessClient.Models;
using DryIoc;
using Prism.Ioc;
using Prism.Regions;
using IceTea.Pure.Utils;

#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8604 // 引用类型参数可能为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.ViewModels;

internal abstract class OnlineChinChessViewModelBase : ChinChessViewModelBase
{
    private const int Seconds = 60;
    private const int TotalSeconds = 300;

    protected HubConnection _signalr;

    protected override string Name => this.IsRedRole == true ? "红色" : "黑色";

    protected virtual bool IsTurnToDo => this.Status == GameStatus.Ready && this.IsRedTurn == this.IsRedRole;

    public OnlineChinChessViewModelBase(IAppConfigFileHotKeyManager appCfgHotKeyManager, IConfigManager configManager) : base(appCfgHotKeyManager)
    {
        try
        {
            AppUtils.TryStartProcess("ChinChessServer.exe");

            this.Status = GameStatus.NotInitialized;

            this.InitSignalR(configManager);

            HeQiCommand = new DelegateCommand(
                HeQi_CommandExecute,
                () => this.Status == GameStatus.Ready
                )
                .ObservesProperty(() => this.Status);

            GiveUpCommand = new DelegateCommand(
                GiveUp_CommandExecute,
                () => this.Status == GameStatus.Ready
                )
                .ObservesProperty(() => this.Status);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async Task InitSignalR(IConfigManager configManager)
    {
        var serverUrl = configManager.ReadConfigNode<string>("ServerUrl".FillToArray());

        _signalr = new HubConnectionBuilder()
            .WithUrl(serverUrl ?? "http://localhost:6666/ChinChess")
            .Build();

        _signalr.On("ReceiveConnected", () => _signalr.InvokeAsync("ClientConnected", this.Mode));

        _signalr.On<bool>("ReceiveRole", isRed => this.IsRedRole = isRed);

        _signalr.On("RecvHeQiReq", () =>
        {
            var tempStatus = this.Status;
            this._status = GameStatus.NotReady;
            RaisePropertyChanged(nameof(Status));

            var result = MessageBox.Show("对方请求和棋，是否同意？", "和棋", MessageBoxButton.YesNo);

            var allowHeQi = result == MessageBoxResult.Yes;
            var msg = "对方请求和棋，已" + (allowHeQi ? "同意" : "拒绝");

            this.PublishMsg(msg);
            this.Log(this.Name, msg, this.IsRedRole == true);

            if (allowHeQi)
            {
                this.Status = GameStatus.Stoped;
            }
            else
            {
                this._status = tempStatus;
                RaisePropertyChanged(nameof(Status));
            }

            return allowHeQi;
        });

        _signalr.On("RecvGiveUpReq", () =>
        {
            var tempStatus = this.Status;
            this._status = GameStatus.NotReady;
            RaisePropertyChanged(nameof(Status));

            var result = MessageBox.Show("对方请求认输，是否同意？", "投降", MessageBoxButton.YesNo);

            var allowGiveUp = result == MessageBoxResult.Yes;
            var msg = "对方请求认输，已" + (allowGiveUp ? "同意" : "拒绝");

            this.PublishMsg(msg);
            this.Log(this.Name, msg, this.IsRedRole == true);

            if (allowGiveUp)
            {
                this.Status = GameStatus.Stoped;
            }
            else
            {
                this._status = tempStatus;
                RaisePropertyChanged(nameof(Status));
            }

            return allowGiveUp;
        });


        _signalr.On("RecvRevokeReq", () =>
        {
            var tempStatus = this.Status;
            this._status = GameStatus.NotReady;
            RaisePropertyChanged(nameof(Status));

            var result = MessageBox.Show("对方请求悔棋，是否同意？", "悔棋", MessageBoxButton.YesNo);

            var allowRevoke = result == MessageBoxResult.Yes;
            var msg = "对方请求悔棋，已" + (allowRevoke ? "同意" : "拒绝");

            this.PublishMsg(msg);
            this.Log(this.Name, msg, this.IsRedRole == true);

            this._status = tempStatus;
            RaisePropertyChanged(nameof(Status));

            return allowRevoke;
        });

        _signalr.On("ReceiveRevoke", () =>
        {
            this.Log(this.Name, "收到回退通知", this.IsRedRole == false);

            base.Revoke_CommandExecute();
        });


        _signalr.On("RecvReplayReq", () =>
        {
            var tempStatus = this.Status;
            this._status = GameStatus.NotReady;
            RaisePropertyChanged(nameof(Status));

            var result = MessageBox.Show("对方请求重玩，是否同意？", "重玩", MessageBoxButton.YesNo);

            var allowReplay = result == MessageBoxResult.Yes;

            if (allowReplay)
            {
                base.RePlay_CommandExecute();
                _signalr.InvokeAsync("PushJieQiDataToClients");
            }
            else
            {
                this._status = tempStatus;
                RaisePropertyChanged(nameof(Status));
            }

            var msg = "对方请求重玩，已" + (allowReplay ? "同意" : "拒绝");

            this.PublishMsg(msg);
            this.Log(this.Name, msg, this.IsRedRole == true);

            return allowReplay;
        });

        _signalr.On("ReceiveWait", () =>
        {
            this.Log(this.Name, "收到对手退出通知", this.IsRedRole == false);

            this.IsRedRole = null;
        });

        // 注册接收消息的方法
        _signalr.On<string>("ReceiveMove", info =>
        {
            var chessInfo = info.DeserializeObject<ChessInfo>();

            if (chessInfo.From == chessInfo.To)
            {
                this.Log(this.Name, $"收到选中通知{chessInfo.To}", this.IsRedRole == false);
            }
            else
            {
                if (_canPutVisitor != null)
                {
                    var action = _canPutVisitor.GetChessData(chessInfo.To.Row, chessInfo.To.Column).IsEmpty
                                    ? "移动" : "吃子";
                    this.Log(this.Name, $"收到{action}通知{chessInfo.From}=>{chessInfo.To}", this.IsRedRole == false);
                }
            }

            var data = _canPutVisitor.GetChess(chessInfo.To.Row, chessInfo.To.Column);
            this.SelectOrPutCommand.Execute(data);
        });

        try
        {
            await _signalr.StartAsync();
        }
        catch (Exception ex)
        {
            var regionManager = ContainerLocator.Current.Resolve<IRegionManager>();
            regionManager.Regions["ChinChessRegion"].NavigationService.Journal.GoBack();

            MessageBox.Show($"服务器未启动..{AppStatics.NewLineChars}{ex.Message}");
        }
    }

    protected override void InitHotKeysCore(IAppHotKeyGroup appHotkeyGroup)
    {
        base.InitHotKeysCore(appHotkeyGroup);

        appHotkeyGroup.TryRegister(new AppHotKey("认输", Key.F, ModifierKeys.Alt));

        appHotkeyGroup.TryRegister(new AppHotKey("和棋", Key.H, ModifierKeys.Alt));
    }

    #region Timer
    protected override void Timer_Tick(object sender, EventArgs e)
    {
        bool isRedTimeout = this.RedSeconds <= 0 || this.TotalRedSeconds <= 0;
        if (isRedTimeout)
        {
            this.ShowWinner(false, true);

            return;
        }

        bool isBlackTimeout = this.BlackSeconds <= 0 || this.TotalBlackSeconds <= 0;
        if (isBlackTimeout)
        {
            this.ShowWinner(true, true);

            return;
        }

        base.Timer_Tick(sender, e);
    }

    private int _blackSeconds;
    public int BlackSeconds
    {
        get => _blackSeconds;
        protected set => SetProperty<int>(ref _blackSeconds, value);
    }

    private int _redSeconds;
    public int RedSeconds
    {
        get => _redSeconds;
        protected set => SetProperty<int>(ref _redSeconds, value);
    }
    #endregion

    private async void HeQi_CommandExecute()
    {
        var tempStatus = this.Status;
        this._status = GameStatus.NotReady;
        RaisePropertyChanged(nameof(Status));

        bool allowGiveUp = await _signalr.InvokeAsync<bool>("RequestHeQi");

        if (allowGiveUp)
        {
            this.Status = GameStatus.Stoped;
        }
        else
        {
            this._status = tempStatus;
            RaisePropertyChanged(nameof(Status));
        }

        var msg = "请求和棋，对方" + (allowGiveUp ? "同意" : "拒绝");

        this.PublishMsg(msg);
        this.Log(this.Name, msg, this.IsRedRole == true);
    }

    private async void GiveUp_CommandExecute()
    {
        var tempStatus = this.Status;
        this._status = GameStatus.NotReady;
        RaisePropertyChanged(nameof(Status));

        bool allowGiveUp = await _signalr.InvokeAsync<bool>("RequestGiveUp");

        if (allowGiveUp)
        {
            this.Status = GameStatus.Stoped;
        }
        else
        {
            this._status = tempStatus;
            RaisePropertyChanged(nameof(Status));
        }

        var msg = "请求认输，对方" + (allowGiveUp ? "同意" : "拒绝");

        this.PublishMsg(msg);
        this.Log(this.Name, msg, this.IsRedRole == true);
    }

    #region override
    protected override void OnGameStatusChanged(GameStatus newStatus)
    {
        base.OnGameStatusChanged(newStatus);

        switch (newStatus)
        {
            case GameStatus.Ready:
                this.Angle = this.IsRedRole == false ? 180 : 0;

                this.RedSeconds = this.BlackSeconds = Seconds;
                this.TotalRedSeconds = this.TotalBlackSeconds = TotalSeconds;
                break;
            default:
                break;
        }
    }

    protected override void SelectOrPut_CommandExecute(ChinChessModel model)
    {
        if (!this.SelectOrPut_CommandExecuteCore(model))
        {
            return;
        }

        var targetIsEmpty = model.Data.IsEmpty;
        // 选中
        bool canSelect = !model.Data.IsEmpty && model.Data.IsRed == this.IsRedTurn;
        if (canSelect)
        {
            if (model.TrySelect(_preMoveVisitor))
            {
                CurrentChess = model;

                this.Select_Mp3();

                if (this.IsTurnToDo)
                {
                    this.Log(this.Name, $"选中{model.Pos}", this.IsRedRole == true);

                    _signalr.InvokeAsync("Move", new ChessInfo
                    {
                        FromRed = this.IsRedRole == true,
                        From = CurrentChess.Pos,
                        To = model.Pos,
                    }.SerializeObject());
                }
            }

            return;
        }

        // 移动棋子到这里 或 吃子
        if (this.CurrentChess.IsNotNullAnd(c => this.TryPutTo(c, model.Pos)))
        {
            if (this.IsTurnToDo)
            {
                var action = targetIsEmpty ? "移动" : "吃子";
                this.Log(this.Name, $"{action}{CurrentChess.Pos}=>{model.Pos}", this.IsRedRole == true);

                _signalr.InvokeAsync("Move", new ChessInfo
                {
                    FromRed = this.IsRedRole == true,
                    From = CurrentChess.Pos,
                    To = model.Pos
                }.SerializeObject());
            }

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
        }
    }

    protected override async void Revoke_CommandExecute()
    {
        var tempStatus = this.Status;
        this._status = GameStatus.NotReady;
        RaisePropertyChanged(nameof(Status));

        bool allowRevoke = await _signalr.InvokeAsync<bool>("RequestRevoke");

        this.PublishMsg("悔棋请求被" + (allowRevoke ? "同意" : "拒绝"));

        if (!allowRevoke)
        {
            this.Log(this.Name, "悔棋请求被拒绝", this.IsRedRole == true);
        }
        else
        {
            base.Revoke_CommandExecute();

            this.Log(this.Name, "回退", this.IsRedRole == true);

            _signalr.InvokeAsync("Revoke");
        }

        this._status = tempStatus;
        RaisePropertyChanged(nameof(Status));
    }

    protected override async void RePlay_CommandExecute()
    {
        var tempStatus = this.Status;
        this._status = GameStatus.NotReady;
        RaisePropertyChanged(nameof(Status));

        bool allowReplay = await _signalr.InvokeAsync<bool>("RequestReplay");

        this.PublishMsg("重玩请求被" + (allowReplay ? "同意" : "拒绝"));

        if (!allowReplay)
        {
            this.Log(this.Name, "重玩请求被拒绝", this.IsRedRole == true);

            this._status = tempStatus;
            RaisePropertyChanged(nameof(Status));
        }
        else
        {
            base.RePlay_CommandExecute();

            this.Log(this.Name, "重玩", this.IsRedRole == true);
        }
    }
    #endregion

    #region Porps
    private bool? _isRedRole;
    public bool? IsRedRole
    {
        get => _isRedRole;
        private set
        {
            if (SetProperty<bool?>(ref _isRedRole, value))
            {
                this.Log(this.Name, value != null ? "获得角色" : "失去角色", this.IsRedRole == true);

                if (this.IsRedRole.HasValue)
                {
                    base.RePlay_CommandExecute();
                }
                else
                {
                    this.Status = GameStatus.NotInitialized;
                }
            }
        }
    }

    protected override void OnTurnChanged(bool newValue)
    {
        base.OnTurnChanged(newValue);

        if (newValue)
        {
            this.BlackSeconds = Seconds;
        }
        else
        {
            this.RedSeconds = Seconds;
        }
    }

    #endregion

    public ICommand HeQiCommand { get; private set; }
    public ICommand GiveUpCommand { get; private set; }

    protected override void DisposeCore()
    {
        GiveUpCommand = null;
        HeQiCommand = null;

        _signalr.DisposeAsync();
        _signalr = null;

        this.Log(this.Name, "退出房间..", this.IsRedRole == true);
        this.IsRedRole = null;

        base.DisposeCore();
    }
}
