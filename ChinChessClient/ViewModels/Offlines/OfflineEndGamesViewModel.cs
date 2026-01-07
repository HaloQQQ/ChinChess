using ChinChessClient.Contracts.Events;
using ChinChessClient.ViewModels.Contracts;
using ChinChessCore.Models;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Windows.Input;
using Prism.Ioc;

namespace ChinChessClient.ViewModels;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
internal class OfflineEndGamesViewModel : NavigateViewModelBase
{
    private List<EndGameModel> _games = new();
    public IReadOnlyList<EndGameModel> EndGames => _games;

    public override string Title => "残局挑战";

    public OfflineEndGamesViewModel(IConfigManager configManager, IRegionManager regionManager, IEventAggregator eventAggregator)
    {
        var endGames = configManager.ReadConfigNode<IDictionary<string, EndGameModel>>("EndGames".FillToArray(), new Dictionary<string, EndGameModel>());

        foreach (var game in endGames)
        {
            game.Value.Name = game.Key;
            _games.Add(game.Value);
        }

        this.GoToPlayManualCommand = new DelegateCommand<EndGameModel>(
            model => regionManager.RequestNavigate("ChinChessRegion", "OfflineCustom",
            nr => eventAggregator.GetEvent<MainTitleChangedEvent>().Publish($"残局挑战-{model.Name}"),
            new NavigationParameters()
            {
                {"EndGame", model },
                {"NeedWarn", _needWarn }
            })
        );

        this.GoToPlayAnswerCommand = new DelegateCommand<EndGameModel>(
            model => regionManager.RequestNavigate("ChinChessRegion", "OfflineAnswer",
            nr => eventAggregator.GetEvent<MainTitleChangedEvent>().Publish($"残局观摩-{model.Name}"),
            new NavigationParameters()
            {
                {"EndGame", model },
                {"NeedWarn", _needWarn }
            })
        );
    }

    public override void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
    {
        continuationCallback(true);
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        this.Dispose();
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        ContainerLocator.Current.Resolve<IEventAggregator>().GetEvent<MainTitleChangedEvent>().Publish(this.Title);
    }

    public ICommand GoToPlayManualCommand { get; private set; }
    public ICommand GoToPlayAnswerCommand { get; private set; }


    protected override void DisposeCore()
    {
        base.DisposeCore();

        this._games.Clear();
        this._games = null;

        this.GoToPlayManualCommand = null;
        this.GoToPlayAnswerCommand = null;
    }
}