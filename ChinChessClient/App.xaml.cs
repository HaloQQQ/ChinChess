using ChinChessClient.ViewModels;
using ChinChessClient.Views;
using IceTea.Pure.Contracts;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.Configs;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System.Windows;

#pragma warning disable CS8603 // 可能返回 null 引用。
namespace ChinChessClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication
{
    protected override Window CreateShell()
    {
        WpfAtomUtils.SwitchTheme(
            "pack://application:,,,/IceTea.Wpf.Core;component/Resources/LightTheme.xaml",
            "pack://application:,,,/IceTea.Wpf.Core;component/Resources/DarkTheme.xaml");

        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IConfigManager, WpfJsonConfigManager>();
        containerRegistry.RegisterSingleton<IAppConfigFileHotKeyManager, AppConfigFileHotKeyManager>();

        containerRegistry.RegisterForNavigation<OnlineChinChessView>("Online");
        containerRegistry.RegisterForNavigation<OnlineJieQiView>("OnlineJieQi");

        containerRegistry.RegisterForNavigation<OfflineChinChessView>("Offline");
        containerRegistry.RegisterForNavigation<OfflineJieQiView>("OfflineJieQi");

        containerRegistry.RegisterSingleton<MainViewModel>();

        var regionManager = Container.Resolve<IRegionManager>();

        regionManager.RegisterViewWithRegion<MainView>("ChinChessRegion");

        ViewModelLocationProvider.Register<MainWindow, MainViewModel>();
    }
}
