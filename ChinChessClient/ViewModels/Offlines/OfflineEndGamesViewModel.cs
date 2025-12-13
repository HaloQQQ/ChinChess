using ChinChessClient.Contracts.Events;
using ChinChessClient.ViewModels.Contracts;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Windows.Input;

namespace ChinChessClient.ViewModels;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
internal class OfflineEndGamesViewModel : NavigateViewModelBase
{
    private List<EndGameModel> _games = new();
    public IReadOnlyList<EndGameModel> EndGames => _games;

    public override string Title => "残局挑战";

    public OfflineEndGamesViewModel(IConfigManager configManager, IRegionManager regionManager, IEventAggregator eventAggregator)
    {
        var endGames = configManager.ReadConfigNode<IDictionary<string, string>>("EndGames".FillToArray(), new Dictionary<string, string>());

        foreach (var game in endGames)
        {
            _games.Add(new EndGameModel(game.Key, game.Value));
        }

        this.GoToPlayManualCommand = new DelegateCommand<EndGameModel>(
            model => regionManager.RequestNavigate("ChinChessRegion", "Offline",
            nr => eventAggregator.GetEvent<MainTitleChangedEvent>().Publish($"{this.Title}-{model.Name}"),
            new NavigationParameters()
            {
                {"EndGame", model }
            })
        );

        this.GoToPlayAutoCommand = new DelegateCommand<EndGameModel>(
            model => regionManager.RequestNavigate("ChinChessRegion", "OfflineAuto",
            nr => eventAggregator.GetEvent<MainTitleChangedEvent>().Publish($"{this.Title}-{model.Name}"),
            new NavigationParameters()
            {
                {"EndGame", model }
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

    public ICommand GoToPlayManualCommand { get; private set; }
    public ICommand GoToPlayAutoCommand { get; private set; }


    protected override void DisposeCore()
    {
        base.DisposeCore();

        this._games.Clear();
        this._games = null;

        this.GoToPlayManualCommand = null;
        this.GoToPlayAutoCommand = null;
    }
}

internal class EndGameModel
{
    public EndGameModel(string name, string data)
    {
        Name = name.AssertArgumentNotNull(nameof(name));
        Data = data.AssertArgumentNotNull(nameof(data));
    }

    public string Name { get; }
    public string Data { get; }
}
