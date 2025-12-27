using ChinChessCore.Commands;
using ChinChessCore.Contracts;
using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;
using Prism.Regions;
using System.Windows.Input;

namespace ChinChessClient.ViewModels;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
internal class OfflineAnswerViewModel : OfflineChinChessViewModelBase
{
    public override ChinChessMode Mode => ChinChessMode.OfflineAnswer;

    public OfflineAnswerViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager)
        : base(appCfgHotKeyManager)
    {
        this.Status = EnumGameStatus.Ready;

        this.MoveCommand = new DelegateCommand(() =>
        {
            var movePathStr = this._movePaths[_currentStep];

            MovePath movePath = ChinChessMovePathTranslator.FromNotation(movePathStr, this.Datas);

            var from = this.Datas[movePath.From.Index];

            if (from.TrySelect(_preMoveVisitor))
            {
                CurrentChess = from;

                var to = this.Datas[movePath.To.Index];
                if (this.TryPutTo(to))
                {
                    _currentStep++;

                    if (this.Result == EnumGameResult.During && _currentStep >= this._movePaths.Count)
                    {
                        this.Result = EnumGameResult.Deuce;
                    }
                }
            }
        }, () => this.Status == EnumGameStatus.Ready)
            .ObservesProperty(() => Status);
    }


    private int _currentStep;

    private IList<string> _movePaths;

    private IList<ChinChessInfo> _originDatas;
    protected override void InitDatas()
    {
        if (!_originDatas.IsNullOrEmpty() && !_movePaths.IsNullOrEmpty())
        {
            _currentStep = 0;

            foreach (var item in this.Datas)
            {
                item.Dispose();
            }

            this.Datas.Clear();

            for (int row = 0; row < 10; row++)
            {
                for (int column = 0; column < 9; column++)
                {
                    this.Datas.Add(new ChinChessModel(row, column));
                }
            }

            foreach (var item in _originDatas)
            {
                this.Datas[item.Pos.Index].Reload(item);
            }
        }
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        var parameters = navigationContext.Parameters;

        if (parameters.TryGetValue<EndGameModel>("EndGame", out EndGameModel data))
        {
            this._originDatas = ChinChessSerializer.Deserialize(data.Datas);

            this._movePaths = data.Steps.Split(",").ToList();

            this.InitDatas();
        }
    }

    protected override void Revoke_CommandExecute()
    {
        TryRevoke();
        this.CurrentChess = null;

        foreach (var item in this.Datas)
        {
            item.IsReadyToPut = false;
        }

        this.IsGameOver();

        this.Log(this.Name, "回退", this.IsRedTurn == true);

        _currentStep--;

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

    public ICommand MoveCommand { get; private set; }

    protected override void DisposeCore()
    {
        base.DisposeCore();

        this.MoveCommand = null;
        if (this._originDatas.IsNotNullAnd())
        {
            this._originDatas.Clear();
        }

        if (this._movePaths.IsNotNullAnd())
        {
            this._movePaths.Clear();
        }
    }
}
