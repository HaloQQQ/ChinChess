using ChinChessClient.Views;
using IceTea.Pure.Contracts;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.Configs;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;
using Prism.Services.Dialogs;
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

        Container.Resolve<IDialogService>().ShowDialog("象棋");

        Window shell = (Window)Container.Resolve<IDialogWindow>();

        RegionManager.SetRegionManager(shell, Container.Resolve<IRegionManager>());

        return default;
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IConfigManager, WpfYamlConfigManager>();
        containerRegistry.RegisterSingleton<IAppConfigFileHotKeyManager, AppConfigFileHotKeyManager>();

        containerRegistry.RegisterDialog<ChinChessView>("象棋");
    }
}
