using ChinChessClient.Models;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.ViewModels;

internal abstract class GameBaseViewModel<T> : NotifyBase, IDialogAware where T : NotifyBase
{
    protected virtual string GameName { get; }

    private IAppConfigFileHotKeyManager _appCfgHotKeyManager;

    public GameBaseViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager, IConfigManager configManager, IEventAggregator eventAggregator)
    {
        _appCfgHotKeyManager = appCfgHotKeyManager.AssertNotNull(nameof(IAppConfigFileHotKeyManager));
        _eventAggregator = eventAggregator.AssertNotNull(nameof(IEventAggregator));

        this.LoadConfig(configManager);

        this.Records = new ObservableCollection<Record>();
        this.Datas = new ObservableCollection<T>();
        this.InitDatas();

        this.InitHotKeys(appCfgHotKeyManager);

        TogglePauseCommand = new DelegateCommand(
            StartPause_CommandExecute,
            () => !IsGameOver && IsStarted)
        .ObservesProperty(() => IsGameOver)
        .ObservesProperty(() => IsStarted);

        RePlayCommand = new DelegateCommand(
            RePlay_CommandExecute,
            () => IsStarted)
        .ObservesProperty(() => IsStarted);

        this.Begin_Wav();
    }

    #region Logicals
    protected abstract bool CheckGameOver();

    protected void InitHotKeys(IAppConfigFileHotKeyManager appCfgFileHotkeyManager)
    {
        var groupName = GameName;

        appCfgFileHotkeyManager.TryRegister(groupName, new[] { "HotKeys", "App", groupName });

        var group = appCfgFileHotkeyManager[groupName];

        group.TryRegister(new AppHotKey("重玩", Key.R, ModifierKeys.Alt));
        group.TryRegister(new AppHotKey("播放/暂停", Key.Space, ModifierKeys.None));

        this.InitHotKeysCore(group.As<IAppHotKeyGroup>());

        KeyGestureDic = group.ToDictionary(hotKey => hotKey.Name);
    }

    /// <summary>
    /// 注册快捷键
    /// </summary>
    /// <param name="appHotkeyGroup"></param>
    protected virtual void InitHotKeysCore(IAppHotKeyGroup appHotkeyGroup)
    {
    }

    protected abstract void InitDatas();

    protected virtual void LoadConfig(IConfigManager configManager) { }

    protected virtual void StartPause_CommandExecute()
    {
        this.IsPaused = !this.IsPaused;
    }

    protected virtual void RePlay_CommandExecute()
    {
        this.IsGameOver = false;

        this.Restart_Wav();
    }
    #endregion

    #region Commons
    private MediaPlayer _player = new MediaPlayer();
    protected void PlayMedia(string mediaName)
    {
        WpfAtomUtils.BeginInvoke(() =>
        {
            _player.Open(new Uri(Path.Combine(AppStatics.ExeDirectory, "Resources/Medias", mediaName), UriKind.RelativeOrAbsolute));
            _player.Play();
        });
    }

    protected void Begin_Wav() => this.PlayMedia("begin.wav");
    protected void Move_Wav() => this.PlayMedia("move.wav");
    protected void Over_Wav() => this.PlayMedia("over.wav");
    protected void Restart_Wav() => this.PlayMedia("restart.wav");


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

    #region Fields
    protected readonly IEventAggregator _eventAggregator;
    #endregion

    #region Props
    public Dictionary<string, IHotKey<Key, ModifierKeys>> KeyGestureDic { get; private set; }

    public IList<T> Datas { get; private set; }

    public IList<Record> Records { get; private set; }

    public abstract bool IsStarted { get; }

    private bool _isPaused = true;
    public bool IsPaused
    {
        get => _isPaused;
        protected set
        {
            if (SetProperty(ref _isPaused, value))
            {
                this.OnIsPausedChanged(value);
            }
        }
    }

    protected virtual void OnIsPausedChanged(bool newValue)
    {
    }

#pragma warning disable CS0067 
    public event Action<IDialogResult> RequestClose;

    private bool _isGameOver;
    public bool IsGameOver
    {
        get => _isGameOver;
        protected set
        {
            if (SetProperty(ref _isGameOver, value) && value)
            {
                this.OnGameOver();
            }

            IsPaused = _isGameOver;
        }
    }

    protected virtual void OnGameOver()
    {
    }

    public string Title => this.GameName;
    #endregion

    #region Commands
    public ICommand RePlayCommand { get; private set; }
    public ICommand TogglePauseCommand { get; private set; }
    #endregion

    public void Log(string name, string action, bool isRed)
        => WpfAtomUtils.BeginInvoke(() => 
        this.Records.Insert(0, new Record(this.Records.Count + 1, name, action, isRed)));

    protected override void DisposeCore()
    {
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

        RePlayCommand = null;
        TogglePauseCommand = null;

        _appCfgHotKeyManager.TryDispose(this.GameName);
        _appCfgHotKeyManager = null;

        base.DisposeCore();
    }
}
