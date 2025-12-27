using ChinChessCore.AutomationEngines;
using ChinChessClient.ViewModels;
using ChinChessClient.Views;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.Configs;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8603 // 可能返回 null 引用。
#pragma warning disable CS8604 // 引用类型参数可能为 null。
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
        containerRegistry.RegisterSingleton<IConfigManager, WpfYamlConfigManager>();
        containerRegistry.RegisterSingleton<IAppConfigFileHotKeyManager, AppConfigFileHotKeyManager>();

        containerRegistry.RegisterForNavigation<OnlineChinChessView>("Online");
        containerRegistry.RegisterForNavigation<OnlineJieQiView>("OnlineJieQi");

        containerRegistry.RegisterForNavigation<OfflineChinChessView>("Offline");
        containerRegistry.RegisterForNavigation<OfflineJieQiView>("OfflineJieQi");

        containerRegistry.RegisterForNavigation<OfflineAutoView>("OfflineAuto");

        containerRegistry.RegisterForNavigation<OfflineCustomView>("OfflineCustom");
        containerRegistry.RegisterForNavigation<OfflineEndGamesView>("OfflineEndGames");
        containerRegistry.RegisterForNavigation<OfflineAnswerView>("OfflineAnswer");


        containerRegistry.RegisterSingleton<MainViewModel>();

        containerRegistry.Register<IEleEyeEngine, EleEyeEngine>();

        var regionManager = Container.Resolve<IRegionManager>();

        regionManager.RegisterViewWithRegion<MainView>("ChinChessRegion");

        ViewModelLocationProvider.Register<MainWindow, MainViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        var eleeyeProcessArr = Process.GetProcessesByName("eleeye");

        foreach (var item in eleeyeProcessArr)
        {
            item.Kill();
        }
    }

    protected override void OnLoadCompleted(NavigationEventArgs e)
    {
        base.OnLoadCompleted(e);

        App.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var list = this.GetMessageList(e.ExceptionObject as Exception);

        var message = $"Domain出现异常:{AppStatics.NewLineChars}" + AppStatics.NewLineChars.Join(list);
        MessageBox.Show(message);
    }

    private IEnumerable<string> GetMessageList(Exception exception)
    {
        var list = new List<string>
            {
                exception.Message
            };

        while ((exception = exception.InnerException) != null)
        {
            list.Add(exception.Message);

            if (!exception.StackTrace.IsNullOrBlank())
            {
                list.Add(exception.StackTrace);
            }
        }

        return list;
    }

    private void Current_DispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var list = this.GetMessageList(e.Exception);

        var message = $"App出现异常:{AppStatics.NewLineChars}" + AppStatics.NewLineChars.Join(list);
        MessageBox.Show(message);
    }
}
