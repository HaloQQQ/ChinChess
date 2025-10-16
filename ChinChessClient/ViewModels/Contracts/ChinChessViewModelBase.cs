using ChinChessClient.Commands;
using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessClient.Visitors;
using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.ViewModels;

internal abstract class ChinChessViewModelBase : GameViewModelBase<ChinChessModel>
{
    public abstract ChinChessMode Mode { get; }

    protected IVisitor _canEatVisitor;
    protected IVisitor _preMoveVisitor;
    private IVisitor _notFatalVisitor;

    protected ChinChessViewModelBase(IAppConfigFileHotKeyManager appCfgHotKeyManager) : base(appCfgHotKeyManager)
    {
        this._canEatVisitor = new CanPutVisitor(this.Datas);
        this._preMoveVisitor = new PreMoveVisitor(this.Datas, _canEatVisitor);
        this._notFatalVisitor = new NotFatalVisitor(this.Datas, _canEatVisitor);
    }

    protected bool SelectOrPutCommand_ExecuteCore(ChinChessModel model)
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
        var target = _canEatVisitor.GetChess(to.Row, to.Column);
        if (target.IsReadyToPut)
        {
            using (new MockMoveCommand(chess, target).Execute())
            {
                var shuai = _canEatVisitor.GetChesses().FirstOrDefault(c => c.Data.Type == ChessType.帥 && c.Data.IsRed == target.Data.IsRed);

                if (shuai != null)
                {
                    if (shuai.Data.FaceToFace(_canEatVisitor)
                        || shuai.Data.IsDangerous(_canEatVisitor, new Position(shuai.Row, shuai.Column), out _))
                    {
                        this.PublishMsg("走子后送将啊，带佬");

                        return false;
                    }
                }
            }

            _canEatVisitor.GetChesses().ForEach(c => c.IsReadyToPut = false);

            return true;
        }

        return false;
    }

    public virtual bool TryPutTo(ChinChessModel chess, Position to)
    {
        if (this.TryPutToCore(chess, to))
        {
            var command = new MoveCommand(
                                    CommandStack.Count + 1,
                                    chess.Data.IsRed == true,
                                    chess, _canEatVisitor.GetChess(to.Row, to.Column)
                                )
                            .Execute();

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

        this.CheckGameOver();
    }

    protected override bool CheckGameOver()
    {
        bool isRedWin = false;

        foreach (ChinChessModel item in this.Datas.Where(c => !c.Data.IsEmpty))
        {
            var isGameOver = false;

            var pos = item.Pos;
            var isDangerous = item.Data.IsDangerous(_canEatVisitor, pos, out ChinChessModel killer);

            if (isDangerous)
            {
                if (!killer.Data.CanBeProtected(
                        _notFatalVisitor,
                        killer.Pos,
                        pos)
                    )
                {
                    if (item.Data is ChinChessShuai shuai)
                    {
                        isRedWin = shuai.IsRed == false;

                        isGameOver = true;
                    }
                }
                else
                {
                    if (item.Data is ChinChessShuai)
                    {
                        this.JiangJun_Mp3();
                    }
                    else
                    {
                        using (new MockMoveCommand(killer, item).Execute())
                        {
                            if (item.Data.IsDangerous(_canEatVisitor, item.Pos, out _))
                            {
                                isDangerous = false;
                            }
                        }
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
        this._notFatalVisitor.Dispose();
        this._notFatalVisitor = null;

        this._preMoveVisitor.Dispose();
        this._preMoveVisitor = null;

        this._canEatVisitor.Dispose();
        this._canEatVisitor = null;

        base.DisposeCore();
    }
}
