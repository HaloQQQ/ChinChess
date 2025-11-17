using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;
using System.Windows.Input;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8601 // 引用类型赋值可能为 null。
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8604 // 引用类型参数可能为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8622 // 参数类型中引用类型的为 Null 性与目标委托不匹配(可能是由于为 Null 性特性)。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessClient.ViewModels;

internal class OnlineChinChessViewModel : OnlineChinChessViewModelBase, IBackup
{
    public override ChinChessMode Mode => ChinChessMode.Online;

    protected override bool IsTurnToDo => !this.IsMock && base.IsTurnToDo;

    public OnlineChinChessViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager, IConfigManager configManager)
        : base(appCfgHotKeyManager, configManager)
    {
        this.RevokeCommand = new DelegateCommand(
            Revoke_CommandExecute,
            () => this.IsTurnToDo && CommandStack.Count > 0
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsMock)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CommandStack.Count);

        this.SelectOrPutCommand = new DelegateCommand<ChinChessModel>(
            SelectOrPut_CommandExecute,
            model => this.Status == GameStatus.Ready
                && (this.IsMock || (!this.IsMock && this.IsRedTurn == this.IsRedRole))
                && model != null && CurrentChess != model
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsMock)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CurrentChess);

        this.MockCommand = new DelegateCommand(() =>
        {
            if (!this.IsMock)
            {
                this.Backup();
            }
            else
            {
                this.Restore();
            }
        }, () => this.Status == GameStatus.Ready && (this.IsMock || this.IsRedTurn == this.IsRedRole))
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.IsMock)
        .ObservesProperty(() => this.Status);
    }

    #region Timer
    protected override void Timer_Tick(object sender, EventArgs e)
    {
        if (!this.IsMock)
        {
            if (this.IsRedTurn)
            {
                this.RedSeconds--;
                this.TotalRedSeconds--;
            }
            else
            {
                this.BlackSeconds--;
                this.TotalBlackSeconds--;
            }
        }
        else
        {
            if (this.IsRedRole == true)
            {
                this.RedSeconds--;
                this.TotalRedSeconds--;
            }
            else
            {
                this.BlackSeconds--;
                this.TotalBlackSeconds--;
            }
        }

        base.Timer_Tick(sender, e);
    }
    #endregion

    public override bool TryPutTo(ChinChessModel chess, Position to)
        => this.IsMock ? this.TryMockPutTo(chess, to) : base.TryPutTo(chess, to);

    #region overrides
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
                    this.Datas.Add(new ChinChessModel(row, column, false));
                }
            }
        });
    }

    protected override void InitHotKeysCore(IAppHotKeyGroup appHotkeyGroup)
    {
        base.InitHotKeysCore(appHotkeyGroup);

        appHotkeyGroup.TryRegister(new AppHotKey("模拟", Key.M, ModifierKeys.Alt));
    }

    protected override void OnTurnChanged(bool newValue)
    {
        if (!this.IsMock)
        {
            base.OnTurnChanged(newValue);
        }
    }
    #endregion

    #region Commands
    public ICommand MockCommand { get; private set; }
    #endregion

    #region 模拟模式
    private bool _isMock;
    public bool IsMock
    {
        get => _isMock;
        private set => SetProperty<bool>(ref _isMock, value);
    }

    public bool TryMockPutTo(ChinChessModel chess, Position to)
    {
        if (this.TryPutToCore(chess, to))
        {
            _canPutVisitor.GetChess(to.Row, to.Column).Data = chess.Data;
            chess.Data = InnerChinChess.Empty;

            return true;
        }

        return false;
    }

    #region IBackup
    private IList<InnerChinChess> _backups;
    private IList<bool> _backupsIsDangerous;
    private IList<bool> _backupsIsReadyToPut;

    private ChinChessModel _backupCurrent;
    private ChinChessModel _backupFrom;
    private ChinChessModel _backupTo;
    private bool _backupIsRedTurn;
    public bool Backup()
    {
        if (!this.IsMock)
        {
            _backups = this.Datas.Select(d => d.Data).ToList();
            _backupsIsDangerous = this.Datas.Select(d => d.IsDangerous).ToList();
            _backupsIsReadyToPut = this.Datas.Select(d => d.IsReadyToPut).ToList();

            _backupCurrent = CurrentChess;
            _backupFrom = From;
            _backupTo = To;
            _backupIsRedTurn = IsRedTurn;

            this.IsMock = true;

            this.Log(this.Name, "进入模拟模式", this.IsRedTurn == true);

            return true;
        }

        return false;
    }

    public bool Restore()
    {
        if (this.IsMock)
        {
            for (int i = 0; i < _backups.Count; i++)
            {
                this.Datas[i].Data = _backups[i];

                this.Datas[i].IsDangerous = _backupsIsDangerous[i];
                this.Datas[i].IsReadyToPut = _backupsIsReadyToPut[i];
            }

            CurrentChess = _backupCurrent;
            From = _backupFrom;
            To = _backupTo;
            IsRedTurn = _backupIsRedTurn;

            this.CleanBackup();

            this.IsMock = false;

            this.Log(this.Name, "退出模拟模式", this.IsRedTurn == true);

            return true;
        }

        return false;
    }

    private void CleanBackup()
    {
        this._backups.IsNotNullAnd(i => { i.Clear(); return true; });
        this._backups = null;

        this._backupsIsDangerous.IsNotNullAnd(i => { i.Clear(); return true; });
        this._backupsIsDangerous = null;

        this._backupsIsReadyToPut.IsNotNullAnd(i => { i.Clear(); return true; });
        this._backupsIsReadyToPut = null;

        this._backupCurrent = null;
        this._backupFrom = null;
        this._backupTo = null;

        this._backupIsRedTurn = false;
    }
    #endregion
    #endregion

    protected override void DisposeCore()
    {
        MockCommand = null;

        this.CleanBackup();

        base.DisposeCore();
    }
}