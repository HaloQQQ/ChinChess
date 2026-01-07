using ChinChessCore.Commands;
using ChinChessCore.Contracts;
using ChinChessCore.Models;
using ChinChessCore.Visitors;
using IceTea.Atom.Extensions;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessClient.ViewModels;

internal abstract class ChinChessViewModelBase : GameViewModelBase<ChinChessModel>
{
    public abstract ChinChessMode Mode { get; }

    public override string Title => this.Mode.GetEnumDescription();

    public ObservableCollection<InnerChinChess> BlackDeads { get; private set; } = new();
    public ObservableCollection<InnerChinChess> RedDeads { get; private set; } = new();

    public string DatasStr => ChinChessSerializer.Serialize(this.Datas.Where(d => !d.Data.IsEmpty)
                                                                .Select(m => new ChinChessInfo(m.Pos, isRed: (bool)m.Data.IsRed, chessType: (ChessType)m.Data.Type))
                                                                .ToArray());

    protected ICanPutToVisitor _canPutVisitor;
    protected IPreMoveVisitor _preMoveVisitor;
    private IGuardVisitor _guardVisitor;

    protected ChinChessViewModelBase(IAppConfigFileHotKeyManager appCfgHotKeyManager) : base(appCfgHotKeyManager)
    {
        this._canPutVisitor = new CanPutToVisitor(this.Datas);
        this._preMoveVisitor = new PreMoveVisitor(this.Datas, _canPutVisitor);
        this._guardVisitor = new GuardVisitor(this.Datas, _canPutVisitor);

        this.ExportDataCommand = new DelegateCommand(ExportDataCommand_CommandExecute, () => this.CommandStack.Count > 0)
            .ObservesProperty(() => this.CommandStack.Count);
    }

    protected virtual void ExportDataCommand_CommandExecute()
    {
        var list = new List<ChinChessModel>();
        for (int row = 0; row < 10; row++)
        {
            for (int column = 0; column < 9; column++)
            {
                list.Add(new ChinChessModel(row, column, false));
            }
        }

        var model = new EndGameModel(DateTime.Now.FormatTime(), this.DatasStr);

        model.Steps = string.Join(',', this.CommandStack.Select(cmd => cmd.Notation));

        var logPath = Path.Combine(AppStatics.ExeDirectory, "Chess.log");

        File.AppendAllText(logPath, model.SerializeObject(Newtonsoft.Json.Formatting.Indented) + AppStatics.NewLineChars);

        this.PublishMsg($"棋局信息已导出到{logPath}");
    }

    protected override void OnGameStatusChanged(EnumGameStatus newStatus)
    {
        base.OnGameStatusChanged(newStatus);

        switch (newStatus)
        {
            case EnumGameStatus.Ready:
                WpfAtomUtils.BeginInvoke(() =>
                {
                    this.RedDeads.Clear();
                    this.BlackDeads.Clear();
                });
                break;
            default:
                break;
        }
    }

    protected virtual bool PushDead(InnerChinChess chess)
    {
        if (chess.IsEmpty)
        {
            return false;
        }

        if (chess.IsRed == true)
        {
            WpfAtomUtils.InvokeAtOnce(() =>
            {
                this.RedDeads.Add(chess);
            });
        }
        else
        {
            WpfAtomUtils.InvokeAtOnce(() =>
            {
                this.BlackDeads.Add(chess);
            });
        }

        return true;
    }

    protected virtual bool ReturnDead(InnerChinChess chess)
    {
        if (chess.IsEmpty)
        {
            return false;
        }

        if (chess.IsRed == true)
        {
            WpfAtomUtils.InvokeAtOnce(() =>
            {
                this.RedDeads.Remove(chess);
            });
        }
        else
        {
            WpfAtomUtils.InvokeAtOnce(() =>
            {
                this.BlackDeads.Remove(chess);
            });
        }

        return true;
    }

    protected bool SelectOrPut_CommandExecuteCore(ChinChessModel model)
    {
        var targetIsEmpty = model.Data.IsEmpty;

        bool isSelected = !targetIsEmpty && model == CurrentChess;
        if (isSelected)
        {
            return false;
        }

        bool hasnotSelected = targetIsEmpty && CurrentChess == null;
        if (hasnotSelected)
        {
            return false;
        }

        return true;
    }

    protected bool TryPutToCore(ChinChessModel chess, Position to)
    {
        var target = _canPutVisitor.GetChess(to);
        if (target.IsReadyToPut)
        {
            using (new MockMoveCommand(chess, target).Execute())
            {
                var shuai = _canPutVisitor.GetChesses().FirstOrDefault(c => c.Data.Type == ChessType.帥 && c.Data.IsRed == target.Data.IsRed);

                if (shuai != null)
                {
                    if (_canPutVisitor.FaceToFace()
                        || shuai.Data.IsDangerous(_canPutVisitor, out _))
                    {
                        this.PublishMsg("走子后送将啊，带佬");

                        return false;
                    }
                }
            }

            this.Datas.ForEach(c => c.IsReadyToPut = false);

            return true;
        }

        return false;
    }

    protected virtual void PreMove(ChinChessModel chess) { }

    public virtual bool TryPutTo(ChinChessModel chess, Position to)
    {
        if (this.TryPutToCore(chess, to))
        {
            this.PreMove(chess);

            var command = new MoveCommand(
                                    CommandStack.Count + 1,
                                    chess.Data.IsRed == true,
                                    chess, _canPutVisitor.GetChess(to),
                                    this.Datas,
                                    this.PushDead,
                                    this.ReturnDead
                                );

            command.Execute();

            WpfAtomUtils.InvokeAtOnce(() => CommandStack.Insert(0, command));

            return true;
        }

        return false;
    }

    protected override void Revoke_CommandExecute()
    {
        base.Revoke_CommandExecute();

        foreach (var item in this.Datas)
        {
            item.IsReadyToPut = false;
        }

        this.IsGameOver();
    }

    protected override bool IsGameOver()
    {
        bool isRedWin = false;

        foreach (ChinChessModel item in this.Datas.Where(c => !c.Data.IsEmpty))
        {
            if (!this._needWarn && item.Data.Type != ChessType.帥)
            {
                continue;
            }

            var isGameOver = false;

            var isDangerous = item.Data.IsDangerous(_canPutVisitor, out ChinChessModel killer);

            if (isDangerous)
            {
                if (item.Data.Type != ChessType.帥)
                {
                    using (new MockMoveCommand(killer, item).Execute())
                    {
                        if (item.Data.IsDangerous(_canPutVisitor, out _))
                        {
                            isDangerous = false;
                        }
                    }
                }
                else
                {
                    if (!killer.Data.CanBeSaveFromMe(_guardVisitor, item.Pos))
                    {
                        isRedWin = item.Data.IsRed == false;

                        isGameOver = true;
                    }
                    else
                    {
                        this.JiangJun_Mp3();
                    }
                }
            }
            else
            {
                if (item.Data.Type == ChessType.帥)
                {
                    int count = this.Datas.Count(c => c.Data.IsRed == item.Data.IsRed);

                    if (count == 1 && !item.Data.CanLeave(_canPutVisitor))
                    {
                        isRedWin = item.Data.IsRed == false;

                        isGameOver = true;
                    }
                }
            }

            item.IsDangerous = isDangerous;

            if (isGameOver)
            {
                this.ShowWinner(isRedWin);

                return true;
            }
        }

        return false;
    }

    protected void ShowWinner(bool isRed, bool isTimeout = false)
    {
        this.Result = EnumGameResult.VictoryOrDefeat;

        var actor = isRed ? "红方" : "黑方";

        var action = $"{actor}获胜";
        if (isTimeout)
        {
            action = isRed ? "黑方" : "红方" + "超时," + action;
        }

        this.PublishMsg(action);
        this.Log(actor, action, this.IsRedTurn);
    }


    public ICommand ExportDataCommand { get; private set; }

    protected override void DisposeCore()
    {
        base.DisposeCore();

        ExportDataCommand = null;

        this._guardVisitor.Dispose();
        this._guardVisitor = null;

        this._preMoveVisitor.Dispose();
        this._preMoveVisitor = null;

        this._canPutVisitor.Dispose();
        this._canPutVisitor = null;

        foreach (var item in this.BlackDeads)
        {
            item.Dispose();
        }
        this.BlackDeads.Clear();
        this.BlackDeads = null;

        foreach (var item in this.RedDeads)
        {
            item.Dispose();
        }
        this.RedDeads.Clear();
        this.RedDeads = null;
    }
}
