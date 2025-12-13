using ChinChessClient.Commands;
using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessClient.ViewModels.Contracts;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using IceTea.Wpf.Core.Interactions;
using Prism.Commands;
using Prism.Regions;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.ViewModels;

internal abstract class GameViewModelBase<T> : NavigateViewModelBase, IDialogMessage,
    IDialogAware where T : NotifyBase
{
    private DispatcherTimer _timer;

    public ObservableCollection<MoveCommand> CommandStack { get; private set; }

    private IAppConfigFileHotKeyManager _appCfgHotKeyManager;

    public GameViewModelBase(IAppConfigFileHotKeyManager appCfgHotKeyManager)
    {
        _appCfgHotKeyManager = appCfgHotKeyManager.AssertNotNull(nameof(IAppConfigFileHotKeyManager));

        this.CommandStack = new();

        this.Records = new ObservableCollection<Record>();
        this.Datas = new ObservableCollection<T>();

        this.InitHotKeys(appCfgHotKeyManager);

        this.RePlayCommand = new DelegateCommand(
            RePlay_CommandExecute,
            () => Status != GameStatus.NotInitialized && Status != GameStatus.NotReady)
        .ObservesProperty(() => Status);

        this.SwitchDirectionCommand = new DelegateCommand(
            () => Angle = Angle == 0 ? 180 : 0
            , () => this.Status == GameStatus.Ready)
        .ObservesProperty(() => this.Status);

        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
    }

    protected virtual void Timer_Tick(object sender, EventArgs e)
    {
        if (this.DialogMessage.IsNotNullAnd(m => m.IsEnable))
        {
            this.DialogMessage.Decrease();
        }
    }

    #region Logicals
    protected abstract bool IsGameOver();

    protected void InitHotKeys(IAppConfigFileHotKeyManager appCfgFileHotkeyManager)
    {
        var groupName = this.Title;

        appCfgFileHotkeyManager.TryRegister(groupName, new[] { "HotKeys", "App", groupName });

        var group = appCfgFileHotkeyManager[groupName];

        group.TryRegister(new AppHotKey("重玩", Key.R, ModifierKeys.Alt));
        group.TryRegister(new AppHotKey("悔棋", Key.Z, ModifierKeys.Control));
        group.TryRegister(new AppHotKey("棋盘换向", Key.D, ModifierKeys.Alt));

        this.InitHotKeysCore(group.As<IAppHotKeyGroup>());

        KeyGestureDic = group.ToDictionary(hotKey => hotKey.Name);
    }

    /// <summary>
    /// 注册快捷键
    /// </summary>
    /// <param name="appHotkeyGroup"></param>
    protected virtual void InitHotKeysCore(IAppHotKeyGroup appHotkeyGroup) { }

    protected abstract void InitDatas();

    protected virtual void RePlay_CommandExecute()
    {
        this.Status = GameStatus.Ready;

        this.Restart_Wav();

        foreach (var item in this.CommandStack)
        {
            item.Destory();
        }

        WpfAtomUtils.BeginInvoke(this.CommandStack.Clear);

        this.InitDatas();
    }
    #endregion

    #region Commons
    private MediaPlayer _player = new MediaPlayer();
    protected void PlayMedia(string mediaName)
    {
        WpfAtomUtils.BeginInvoke(() =>
        {
            if (_player.IsNotNullAnd())
            {
                _player.Open(new Uri(Path.Combine(AppStatics.ExeDirectory, "Resources/Medias", mediaName), UriKind.RelativeOrAbsolute));
                _player.Play();
            }
        });
    }

    protected void Eat_Mp3() => this.PlayMedia("eat.mp3");
    protected void Go_Mp3() => this.PlayMedia("go.mp3");
    protected void JiangJun_Mp3() => this.PlayMedia("jiangjun.mp3");
    protected void Select_Mp3() => this.PlayMedia("select.mp3");

    protected void Begin_Wav() => this.PlayMedia("begin.wav");
    protected void Move_Wav() => this.PlayMedia("move.wav");
    protected void Over_Wav() => this.PlayMedia("over.wav");
    protected void Restart_Wav() => this.PlayMedia("restart.wav");

    public void Log(string name, string action, bool isRed)
        => WpfAtomUtils.BeginInvoke(() =>
        this.Records?.Insert(0, new Record(this.Records.Count + 1, name, action, isRed)));

    protected void PublishMsg(string message)
        => DialogMessage = new DialogMessage(message.AssertNotNull(nameof(DialogMessage)));
    #endregion

    #region Props
    protected abstract string Name { get; }

    protected bool _needWarn;

    public Dictionary<string, IHotKey<Key, ModifierKeys>> KeyGestureDic { get; private set; }

    public IList<T> Datas { get; private set; }

    public IList<Record> Records { get; private set; }

    protected GameStatus _status;
    public GameStatus Status
    {
        get => _status;
        protected set
        {
            if (SetProperty<GameStatus>(ref _status, value) || value == GameStatus.Ready)
            {
                OnGameStatusChanged(value);
            }
        }
    }
    protected virtual void OnGameStatusChanged(GameStatus newStatus)
    {
        switch (newStatus)
        {
            case GameStatus.Ready:
                CurrentChess = null;
                IsRedTurn = true;

                this._timer.IsNotNullAnd(t => t.IsEnabled = true);
                break;
            case GameStatus.NotInitialized:
            case GameStatus.Stoped:
                this._timer.IsNotNullAnd(t => t.IsEnabled = false);
                break;
            default:
                break;
        }
    }

    private double _angle;
    public double Angle
    {
        get => _angle;
        protected set => SetProperty(ref _angle, value);
    }

    private int _totalBlackSeconds;
    public int TotalBlackSeconds
    {
        get => _totalBlackSeconds;
        protected set
        {
            if (SetProperty<int>(ref _totalBlackSeconds, value))
            {
                RaisePropertyChanged(nameof(BlackTimeSpan));
            }
        }
    }

    public string BlackTimeSpan => TimeSpan.FromSeconds(_totalBlackSeconds).FormatTimeSpan();

    private int _totalRedSeconds;
    public int TotalRedSeconds
    {
        get => _totalRedSeconds;
        protected set
        {
            if (SetProperty<int>(ref _totalRedSeconds, value))
            {
                RaisePropertyChanged(nameof(RedTimeSpan));
            }
        }
    }

    public string RedTimeSpan => TimeSpan.FromSeconds(_totalRedSeconds).FormatTimeSpan();

    private T _currentChess;
    public T CurrentChess
    {
        get => _currentChess;
        protected set => SetProperty<T>(ref _currentChess, value);
    }

    private T _from;
    public T From
    {
        get => _from;
        protected set => SetProperty<T>(ref _from, value);
    }

    private T _to;
    public T To
    {
        get => _to;
        protected set => SetProperty<T>(ref _to, value);
    }

    private bool _isRedTurn = true;
    public bool IsRedTurn
    {
        get => _isRedTurn;
        protected set
        {
            if (SetProperty(ref _isRedTurn, value))
            {
                this.OnTurnChanged(value);
            }
        }
    }

    protected virtual void OnTurnChanged(bool newValue) { }

    private DialogMessage _dialogMessage;
    public DialogMessage DialogMessage
    {
        get => this._dialogMessage;
        private set => SetProperty<DialogMessage>(ref _dialogMessage, value);
    }
    #endregion

    #region Commands
    public ICommand SelectOrPutCommand { get; protected set; }

    protected abstract void SelectOrPut_CommandExecute(T model);

    public ICommand RevokeCommand { get; protected set; }
    protected virtual void Revoke_CommandExecute()
    {
        TryRevoke();
        TryRevoke();
        this.CurrentChess = null;

        bool TryRevoke()
        {
            IChinChessCommand current = this.CommandStack.FirstOrDefault();
            if (current != null)
            {
                this.From = null;
                this.To = this.Datas[current.From.Index];

                this.TryReturnDataToJieQi(current);

                current.Disposer?.Dispose();

                WpfAtomUtils.InvokeAtOnce(() =>
                {
                    this.CommandStack.RemoveAt(0);
                });

                this.IsRedTurn = !this.IsRedTurn;

                return true;
            }

            return false;
        }
    }

    protected virtual void TryReturnDataToJieQi(IChinChessCommand moveCommand) { }

    public ICommand RePlayCommand { get; private set; }

    public ICommand SwitchDirectionCommand { get; private set; }
    #endregion

    protected override void DisposeCore()
    {
        RePlayCommand = null;
        SwitchDirectionCommand = null;
        SwitchDirectionCommand = null;
        RevokeCommand = null;

        _timer.IsEnabled = false;
        _timer.Tick -= Timer_Tick;
        _timer = null;

        this.CurrentChess = null;
        this.From = null;
        this.To = null;

        foreach (var item in this.CommandStack)
        {
            item.Destory();
        }
        this.CommandStack.Clear();
        this.CommandStack = null;

        foreach (var item in this.Datas)
        {
            item.Dispose();
        }
        this.Datas.Clear();
        this.Datas = null;

        this.Records.Clear();
        this.Records = null;

        KeyGestureDic.Clear();
        KeyGestureDic = null;

        _player.Close();
        _player = null;

        _appCfgHotKeyManager.TryDispose(this.Title);
        _appCfgHotKeyManager = null;

        base.DisposeCore();
    }

    #region IDialogAware

#pragma warning disable CS0067
    public event Action<IDialogResult> RequestClose;

    public bool CanCloseDialog()
    {
        return true;
    }

    public void OnDialogClosed()
    {
        this.Dispose();
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }
    #endregion

    #region IConfirmNavigationRequest
    public override void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
    {
        bool canNavigate = true;

        if (this.Status == GameStatus.Ready)
        {
            canNavigate = MessageBox.Show("游戏没结束，确定要离开？", "警告", MessageBoxButton.YesNo) == MessageBoxResult.Yes;

            if (canNavigate)
            {
                this.Dispose();
            }
        }

        continuationCallback(canNavigate);
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        if (this.Status != GameStatus.Ready)
        {
            this.Dispose();
        }
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        if (navigationContext.Parameters.ContainsKey("NeedWarn"))
        {
            this._needWarn = (bool)navigationContext.Parameters["NeedWarn"];
        }
    }
    #endregion

}
