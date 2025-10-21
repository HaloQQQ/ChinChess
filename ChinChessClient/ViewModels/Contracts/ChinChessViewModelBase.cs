using ChinChessClient.Commands;
using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessClient.Visitors;
using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using System.Collections.ObjectModel;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.ViewModels;

internal abstract class ChinChessViewModelBase : GameViewModelBase<ChinChessModel>
{
    public abstract ChinChessMode Mode { get; }

    public ObservableCollection<InnerChinChess> BlackDeads { get; private set; } = new();
    public ObservableCollection<InnerChinChess> RedDeads { get; private set; } = new();

    protected CanPutVisitor _canPutVisitor;
    protected IPreMoveVisitor _preMoveVisitor;
    private IGuardVisitor _guardVisitor;

    protected ChinChessViewModelBase(IAppConfigFileHotKeyManager appCfgHotKeyManager) : base(appCfgHotKeyManager)
    {
        this._canPutVisitor = new CanPutVisitor(this.Datas);
        this._preMoveVisitor = new PreMoveVisitor(this.Datas, _canPutVisitor);
        this._guardVisitor = new GuardVisitor(this.Datas, _canPutVisitor);
    }

    protected override void OnGameStatusChanged(GameStatus newStatus)
    {
        base.OnGameStatusChanged(newStatus);

        switch (newStatus)
        {
            case GameStatus.Ready:
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
        var target = _canPutVisitor.GetChess(to.Row, to.Column);
        if (target.IsReadyToPut)
        {
            using (new MockMoveCommand(chess, target).Execute())
            {
                var shuai = _canPutVisitor.GetChesses().FirstOrDefault(c => c.Data.Type == ChessType.帥 && c.Data.IsRed == target.Data.IsRed);

                if (shuai != null)
                {
                    if (_canPutVisitor.FaceToFace()
                        || shuai.Data.IsDangerous(_canPutVisitor, shuai.Pos, out _))
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
                                    chess, _canPutVisitor.GetChess(to.Row, to.Column),
                                    this.PushDead,
                                    this.ReturnDead
                                );

            command.Execute();

            WpfAtomUtils.BeginInvoke(() =>
            {
                CommandStack.Insert(0, command);
            });

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

            var isDangerous = item.Data.IsDangerous(_canPutVisitor, item.Pos, out ChinChessModel killer);

            if (isDangerous)
            {
                if (item.Data.Type != ChessType.帥)
                {
                    using (new MockMoveCommand(killer, item).Execute())
                    {
                        if (item.Data.IsDangerous(_canPutVisitor, item.Pos, out _))
                        {
                            isDangerous = false;
                        }
                    }
                }
                else
                {
                    if (!killer.Data.CanBeProtected(
                            _guardVisitor,
                            killer.Pos,
                            item.Pos)
                       )
                    {
                        if (item.Data is ChinChessShuai shuai)
                        {
                            if (!shuai.SelfRescue(_canPutVisitor, item.Pos))
                            {
                                isRedWin = shuai.IsRed == false;

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
                        if (item.Data is ChinChessShuai)
                        {
                            this.JiangJun_Mp3();
                        }
                    }
                }
            }
            else
            {
                if (item.Data.Type == ChessType.帥)
                {
                    int count = this.Datas.Count(c => c.Data.IsRed == item.Data.IsRed);

                    if (count == 1 && item.Data is ChinChessShuai shuai && !shuai.SelfRescue(_canPutVisitor, item.Pos))
                    {
                        isRedWin = shuai.IsRed == false;

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
        this.Over_Wav();

        this.Status = GameStatus.Stoped;

        var actor = isRed ? "红方" : "黑方";

        var action = $"{actor}获胜";
        if (isTimeout)
        {
            action = isRed ? "黑方" : "红方" + "超时," + action;
        }

        this.PublishMsg(action);
        this.Log(actor, action, this.IsRedTurn);
    }

    protected override void DisposeCore()
    {
        base.DisposeCore();

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
