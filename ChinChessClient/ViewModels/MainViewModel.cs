using ChinChessClient.Contracts;
using ChinChessClient.Views;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using IceTea.Pure.Utils.Events;
using IceTea.Wpf.Atom.Contracts.FileFilters;
using IceTea.Wpf.Atom.Contracts.MyEvents;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using IceTea.Wpf.Core.Utils;
using Prism.Commands;
using Prism.Regions;
using System.Windows;
using System.Windows.Input;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.ViewModels;

internal class MainViewModel : NotifyBase, INavigationAware
{
    public IAppHotKeyManager AppConfigFileHotKeyManager { get; private set; }

    public MainViewModel(IRegionManager regionManager, IConfigManager configManager, IAppConfigFileHotKeyManager appConfigFileHotKeyManager)
    {
        AppConfigFileHotKeyManager = appConfigFileHotKeyManager.AssertArgumentNotNull(nameof(IAppHotKeyManager));

        this.NavigateToCommand = new DelegateCommand<string>(
            uri => regionManager.RequestNavigate("ChinChessRegion", uri, nr =>
            {
                if (nr.IsNotNullAnd(_ => _.Context.Parameters.ContainsKey("Title")))
                {
                    this.Title = nr.Context.Parameters["Title"].As<string>();
                }
            }, new NavigationParameters()
            {
                {nameof(NeedWarn), NeedWarn }
            })
        );

        this.GoBackCommand = new DelegateCommand(
            () => regionManager.Regions["ChinChessRegion"].NavigationService.Journal.GoBack()
        , () => this.CurrentPage != null && this.CurrentPage != nameof(MainView))
        .ObservesProperty(() => this.CurrentPage);

        this.ForwardCommand = new DelegateCommand(
            () => regionManager.Regions["ChinChessRegion"].NavigationService.Journal.GoForward()
        , () => this.CurrentPage != null && this.CurrentPage == nameof(MainView))
        .ObservesProperty(() => this.CurrentPage);

        this.ChooseBackCommand = new DelegateCommand(() =>
        {
            var fileDialog = WpfAtomUtils.OpenFileDialog(string.Empty, new PictureFilter());

            if (fileDialog != null)
            {
                this.BackImage = new ImageRecord(fileDialog.FileName);
            }
        });

        this.ResetAppHotKeyGroupCommand = new DelegateCommand<IAppConfigFileHotKeyGroup>(appHotKeyGroup =>
        {
            this.IsEditingSetting = false;

            var failedItems = appHotKeyGroup.Reset();
            var message = failedItems.Any() ? $"{string.Join(Environment.NewLine, failedItems.Select(i => i.ToString()))}{Environment.NewLine}重置失败" : "重置成功";

            MessageBox.Show(message);
        });

        CustomEventManager.Current.GetEvent<OpenSettingEvent>().Execute += () =>
            this.IsEditingSetting = !this.IsEditingSetting;

        this.NeedWarn = configManager.ReadConfigNode<bool>(nameof(NeedWarn).FillToArray(), true);
        configManager.SetConfig += config => config.WriteConfigNode<bool>(NeedWarn, nameof(NeedWarn).FillToArray());
    }

    #region Porps
    private string _title = "主界面";
    public string Title
    {
        get => _title;
        private set => SetProperty<string>(ref _title, value);
    }

    private string _currentPage;
    public string CurrentPage
    {
        get => _currentPage;
        private set => SetProperty<string>(ref _currentPage, value);
    }

    private ImageRecord _backImage = new ImageRecord("../Resources/Images/群山.jpeg");
    public ImageRecord BackImage
    {
        get => _backImage;
        set => SetProperty<ImageRecord>(ref _backImage, value);
    }

    private bool _isEditingSetting;
    public bool IsEditingSetting
    {
        get => _isEditingSetting;
        set => SetProperty<bool>(ref _isEditingSetting, value);
    }

    private bool _needWarn;
    public bool NeedWarn
    {
        get => _needWarn;
        set => SetProperty<bool>(ref _needWarn, value);
    }
    #endregion

    #region Commands
    public ICommand NavigateToCommand { get; private set; }

    public ICommand ChooseBackCommand { get; private set; }

    public ICommand GoBackCommand { get; private set; }
    public ICommand ForwardCommand { get; private set; }

    public ICommand ResetAppHotKeyGroupCommand { get; private set; }
    #endregion

    #region INavigationAware
    /// <summary>
    /// object LoadContent(IRegion region, NavigationContext navigationContext)
    /// </summary>
    /// <param name="navigationContext"></param>
    /// <returns></returns>
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        var journal = navigationContext.NavigationService.Journal;

        if (journal.CurrentEntry is null)
        {
            journal.RecordNavigation(new RegionNavigationJournalEntry()
            {
                Uri = new Uri(nameof(MainView), UriKind.RelativeOrAbsolute),
            }, true);
        }

        this.CurrentPage = navigationContext.Uri.ToString();
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        this.CurrentPage = navigationContext.Uri.ToString();

        this.Title = "主界面";
    }
    #endregion

    protected override void DisposeCore()
    {
        NavigateToCommand = null;
        ChooseBackCommand = null;
        GoBackCommand = null;
        ForwardCommand = null;

        ResetAppHotKeyGroupCommand = null;

        AppConfigFileHotKeyManager = null;

        base.DisposeCore();
    }
}
