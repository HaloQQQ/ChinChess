using ChinChessClient.Commands;
using ChinChessClient.Models;
using ChinChessClient.Visitors;
using ChinChessCore.Models;
using IceTea.Atom.Extensions;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Microsoft.AspNetCore.SignalR.Client;
using Prism.Commands;
using Prism.Events;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8601 // 引用类型赋值可能为 null。
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8604 // 引用类型参数可能为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessClient.ViewModels;

internal class ChinChessViewModel : GameBaseViewModel<ChinChessModel>
{
    private IVisitor _canEatVisitor;
    private IVisitor _preMoveVisitor;
    private IVisitor _notFatalVisitor;

    private HubConnection _signalr;

    protected override string GameName => "象棋";

    private DispatcherTimer _timer;

    private ChinChessModel _currentChess;
    public ChinChessModel CurrentChess
    {
        get => _currentChess;
        private set => SetProperty<ChinChessModel>(ref _currentChess, value);
    }

    public bool CanGoBack { get; }

    public ObservableCollection<IChinChessCommand> Stack { get; private set; }

    public ChinChessViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager, IConfigManager configManager, IEventAggregator eventAggregator)
        : base(appCfgHotKeyManager, configManager, eventAggregator)
    {
        this._canEatVisitor = new CanPutVisitor(this.Datas);
        this._preMoveVisitor = new PreMoveVisitor(this.Datas, _canEatVisitor);
        this._notFatalVisitor = new NotFatalVisitor(this.Datas, _canEatVisitor);

        this.Stack = new();

        this.RevokeCommand = new DelegateCommand(
            Revoke_CommandExecute,
            () => this.CanInvoke && !IsGameOver && !IsPaused && Stack.Count > 0
        )
        .ObservesProperty(() => this.IsStarted)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CanGoBack)
        .ObservesProperty(() => this.IsPaused)
        .ObservesProperty(() => this.IsGameOver);

        this.SelectOrPutCommand = new DelegateCommand<ChinChessModel>(
            model =>
        {
            var targetData = model.Data;
            var targetIsEmpty = targetData.IsEmpty;

            bool isSelected = !targetIsEmpty && model == CurrentChess;
            bool hasnotSelected = targetIsEmpty && CurrentChess == null;
            if (isSelected || hasnotSelected)
            {
                return;
            }

            // 选中
            bool canSelect = !targetIsEmpty && targetData.IsRed == this.IsRedTurn;
            if (canSelect)
            {
                if (model.TrySelect(_preMoveVisitor))
                {
                    CurrentChess = model;

                    this.PlayMedia("select.mp3");

                    if (this.CanInvoke)
                    {
                        this.Log(this.Name, $"选中{new Position(model.Row, model.Column)}", this.IsRedRole == true);

                        _signalr.InvokeAsync("Move", new ChessInfo
                        {
                            FromRed = this.IsRedRole.Value,
                            From = new Position(CurrentChess.Row, CurrentChess.Column),
                            To = new Position(model.Row, model.Column),
                        }.SerializeObject());
                    }
                }

                return;
            }

            // 移动棋子到这里 或 吃子
            if (this.CurrentChess.IsNotNullAnd(c => c.TryPutTo(_canEatVisitor, new Position(model.Row, model.Column), Stack, new Action<string>(this.PublishMsg))))
            {
                if (this.CanInvoke)
                {
                    var action = targetIsEmpty ? "移动" : "吃子";
                    this.Log(this.Name, $"{action}{new Position(this.CurrentChess.Row, this.CurrentChess.Column)}=>{new Position(model.Row, model.Column)}", this.IsRedRole == true);

                    _signalr.InvokeAsync("Move", new ChessInfo
                    {
                        FromRed = this.IsRedRole.Value,
                        From = new Position(CurrentChess.Row, CurrentChess.Column),
                        To = new Position(model.Row, model.Column)
                    }.SerializeObject());
                }

                RaisePropertyChanged(nameof(CanGoBack));

                if (targetIsEmpty)
                {
                    this.PlayMedia("go.mp3");
                }
                else
                {
                    this.PlayMedia("eat.mp3");
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
            model => this.CanInvoke && model != null && CurrentChess != model && !IsGameOver && !IsPaused
        )
        .ObservesProperty(() => this.IsStarted)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CurrentChess)
        .ObservesProperty(() => this.IsPaused)
        .ObservesProperty(() => this.IsGameOver);

        this.SwitchDirectionCommand = new DelegateCommand(
            () => Angle = Angle == 0 ? 180 : 0
            , () => this.IsStarted && !IsGameOver)
        .ObservesProperty(() => this.IsStarted)
        .ObservesProperty(() => this.IsPaused)
        .ObservesProperty(() => this.IsGameOver);

        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;

#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
        this.InitSignalR();
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
    }

    private async Task InitSignalR()
    {
        _signalr = new HubConnectionBuilder()
            .WithUrl("http://localhost:6666/ChinChess")
            .Build();

        _signalr.On<bool>("SetRole", isRed => this.IsRedRole = isRed);

        _signalr.On("ReceiveRevoke", () =>
        {
            this.Log(this.Name, "收到回退通知", this.IsRedRole == false);

            this.Revoke_CommandExecuteCore();
        });

        _signalr.On("ReceiveStartPause", () =>
        {
            this.Log(this.Name, "收到" + (this.IsPaused ? "继续" : "暂停") + "通知", this.IsRedRole == false);

            base.StartPause_CommandExecute();
        });

        _signalr.On("ReceiveRePlay", () =>
        {
            this.Log(this.Name, "收到重玩通知", this.IsRedRole == false);

            this.RePlayCore();
        });

        _signalr.On("ReceiveWait", () =>
        {
            this.Log(this.Name, "收到对手退出通知", this.IsRedRole == false);

            this.RePlayCore();

            this.IsRedRole = null;
        });

        // 注册接收消息的方法
        _signalr.On<string>("ReceiveMove", info =>
        {
            var chessInfo = info.DeserializeObject<ChessInfo>();

            if (chessInfo.From == chessInfo.To)
            {
                this.Log(this.Name, $"收到选中通知{new Position(chessInfo.To.Row, chessInfo.To.Column)}", this.IsRedRole == false);
            }
            else
            {
                if (_canEatVisitor != null)
                {
                    var action = _canEatVisitor.GetChessData(chessInfo.To.Row, chessInfo.To.Column).IsEmpty
                                    ? "移动" : "吃子";
                    this.Log(this.Name, $"收到{action}通知{new Position(chessInfo.From.Row, chessInfo.From.Column)}=>{new Position(chessInfo.To.Row, chessInfo.To.Column)}", this.IsRedRole == false);
                }
            }

            var data = _canEatVisitor.GetChess(chessInfo.To.Row, chessInfo.To.Column);
            this.SelectOrPutCommand.Execute(data);
        });

        await _signalr.StartAsync();

        if (_signalr.State == HubConnectionState.Disconnected)
        {
            this.Dispose();
            MessageBox.Show("服务器未启动..");
        }
    }

    #region Timer
    private void Timer_Tick(object sender, EventArgs e)
    {
        Seconds++;

        if (this.DialogMessage.IsNotNullAnd(m => m.IsEnable))
        {
            this.DialogMessage.Decrease();
        }
    }

    protected override void OnIsPausedChanged(bool newValue)
    {
        if (this._timer != null)
        {
            this._timer.IsEnabled = !newValue;
        }

        base.OnIsPausedChanged(newValue);
    }

    protected override void OnGameOver()
    {
        this.Seconds = 0;

        base.OnGameOver();
    }

    private int _seconds;
    private int Seconds
    {
        get => _seconds;
        set
        {
            if (SetProperty(ref _seconds, value))
            {
                if (IsRedTurn || value == 0)
                {
                    RaisePropertyChanged(nameof(RedTimeSpan));
                }

                if (!IsRedTurn || value == 0)
                {
                    RaisePropertyChanged(nameof(BlackTimeSpan));
                }
            }
        }
    }

    public string BlackTimeSpan => TimeSpan.FromSeconds(_seconds).FormatTimeSpan();

    public string RedTimeSpan => TimeSpan.FromSeconds(_seconds).FormatTimeSpan();
    #endregion

    #region overrides
    protected override bool CheckGameOver()
    {
        string actor = string.Empty;
        var shuais = this.Datas.Where(m => m.Data.Type == ChessType.帥);

        foreach (ChinChessModel item in shuais)
        {
            if (item.Data is ChinChessShuai shuai)
            {
                if (shuai.FaceToFace(_canEatVisitor))
                {
                    actor = this.IsRedTurn ? "黑方" : "红方";

                    this.IsGameOver = true;
                }
                else
                {
                    var pos = new Position(item.Row, item.Column);
                    item.IsDangerous = shuai.IsDangerous(_canEatVisitor, pos, out ChinChessModel killer);

                    if (item.IsDangerous)
                    {
                        if (!killer.Data.Accept(
                                _notFatalVisitor,
                                new Position(killer.Row, killer.Column),
                                pos)
                            )
                        {
                            actor = shuai.IsRed == true ? "黑方" : "红方";

                            this.IsGameOver = true;
                        }
                    }

                }

                if (this.IsGameOver)
                {
                    this.Over_Wav();

                    this.PublishMsg($"{actor}获胜");

                    return true;
                }
            }
        }

        if (!shuais.Any(s => s.IsDangerous))
        {
            this.Datas.ForEach(c => c.IsDangerous = false);
        }

        return false;
    }

    private void Revoke_CommandExecute()
    {
        this.Revoke_CommandExecuteCore();

        this.Log(this.Name, "回退", this.IsRedRole == true);

        _signalr.InvokeAsync("Revoke");
    }

    private void Revoke_CommandExecuteCore()
    {
        var first = TryRevoke();
        var second = TryRevoke();

        if (first || second)
        {
            RaisePropertyChanged(nameof(CanGoBack));
        }

        bool TryRevoke()
        {
            IChinChessCommand current = default;
            if ((current = this.Stack.FirstOrDefault()) != null)
            {
                this.From = null;
                this.To = _canEatVisitor.GetChess(current.From.Row, current.From.Column);

                current.Dispose();

                WpfAtomUtils.InvokeAtOnce(() =>
                {
                    this.Stack.RemoveAt(0);
                });

                IsRedTurn = !IsRedTurn;

                return true;
            }

            return false;
        }
    }

    protected override void StartPause_CommandExecute()
    {
        this.Log(this.Name, this.IsPaused ? "继续" : "暂停", this.IsRedRole == true);

        base.StartPause_CommandExecute();

        _signalr.InvokeAsync("StartPause");
    }

    protected override void RePlay_CommandExecute()
    {
        this.RePlayCore();

        this.Log(this.Name, "重玩", this.IsRedRole == true);

        _signalr.InvokeAsync("RePlay");
    }

    private void RePlayCore()
    {
        base.RePlay_CommandExecute();

        foreach (var item in this.Stack)
        {
            item.Dispose();
        }

        WpfAtomUtils.BeginInvoke(() =>
        {
            this.Stack.Clear();
            RaisePropertyChanged(nameof(CanGoBack));
        });

        this.InitDatas();

        _currentChess = null;
        IsRedTurn = true;

        if (this.IsRedRole.HasValue)
        {
            this.Angle = this.IsRedRole.Value ? 0 : 180;
        }

        Seconds = 0;
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
                    this.Datas.Add(new ChinChessModel(row, column));
                }
            }
        });
    }

    protected override void InitHotKeysCore(IAppHotKeyGroup appHotkeyGroup)
    {
        appHotkeyGroup.TryRegister(new AppHotKey("悔棋", Key.Z, ModifierKeys.Control));
        appHotkeyGroup.TryRegister(new AppHotKey("棋盘换向", Key.D, ModifierKeys.Alt));
    }
    #endregion

    #region Props
    public override bool IsStarted => this.IsRedRole != null;

    public bool CanInvoke => this.IsStarted && this.IsRedTurn == this.IsRedRole;

    private ChinChessModel _from;
    public ChinChessModel From
    {
        get => _from;
        private set => SetProperty<ChinChessModel>(ref _from, value);
    }

    private ChinChessModel _to;
    public ChinChessModel To
    {
        get => _to;
        private set => SetProperty<ChinChessModel>(ref _to, value);
    }

    public string Name => this.IsRedRole == true ? "红色" : "黑色";

    private bool? _isRedRole;
    public bool? IsRedRole
    {
        get => _isRedRole;
        private set
        {
            if (SetProperty<bool?>(ref _isRedRole, value))
            {
                if (this.IsRedRole.HasValue)
                {
                    this.Angle = this.IsRedRole.Value ? 0 : 180;

                    this.Log(this.Name, "获得角色", this.IsRedRole == true);
                }

                this.IsGameOver = (value == null);

                RaisePropertyChanged(nameof(IsStarted));
            }
        }
    }

    private bool _isRedTurn = true;
    public bool IsRedTurn
    {
        get => _isRedTurn;
        private set
        {
            if (SetProperty(ref _isRedTurn, value))
            {
                Seconds = 0;
            }
        }
    }

    private double _angle;
    public double Angle
    {
        get => _angle;
        private set => SetProperty(ref _angle, value);
    }

    private DialogMessage _dialogMessage;
    public DialogMessage DialogMessage
    {
        get => this._dialogMessage;
        private set => SetProperty<DialogMessage>(ref _dialogMessage, value);
    }

    private void PublishMsg(string message)
        => DialogMessage = new DialogMessage(message.AssertNotNull(nameof(DialogMessage)));
    #endregion

    #region Commands
    public ICommand SelectOrPutCommand { get; }

    public ICommand RevokeCommand { get; }

    public ICommand SwitchDirectionCommand { get; }
    #endregion

    protected override void DisposeCore()
    {
        _signalr.StopAsync();
        _signalr = null;

        _timer.IsEnabled = false;
        _timer.Tick -= Timer_Tick;
        _timer = null;

        foreach (var item in this.Stack)
        {
            item.Dispose();
        }
        this.Stack.Clear();
        this.Stack = null;

        this.CurrentChess = null;
        this.From = null;
        this.To = null;

        this.Log(string.Empty, "退出房间，等待匹配", this.IsRedRole == true);
        this._isRedRole = null;

        this._notFatalVisitor.Dispose();
        this._notFatalVisitor = null;

        this._preMoveVisitor.Dispose();
        this._preMoveVisitor = null;

        this._canEatVisitor.Dispose();
        this._canEatVisitor = null;

        base.DisposeCore();
    }
}