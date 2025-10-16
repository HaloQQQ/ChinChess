using ChinChessClient.Commands;
using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Atom.Extensions;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Microsoft.AspNetCore.SignalR.Client;
using Prism.Commands;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.ViewModels;

internal class OnlineJieQiViewModel : OnlineChinChessViewModelBase
{
    public override string Title => "揭棋在线版";

    public override ChinChessMode Mode => ChinChessMode.OnlineJieQi;

    public OnlineJieQiViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager, IConfigManager configManager)
        : base(appCfgHotKeyManager, configManager)
    {
        _signalr.On<IEnumerable<ChessType>>("ReceiveJieQi", seq =>
        {
            this.Log(this.Name, "收到揭棋序列", this.IsRedRole == false);

            _black = seq.Take(15).Select(t => Get(t, false)).ToList();
            _red = seq.Skip(15).Select(t => Get(t, true)).ToList();

            InnerChinChess Get(ChessType type, bool isRed)
            {
                switch (type)
                {
                    case ChessType.炮:
                        return new ChinChessPao(isRed, isJieQi: true, isBack: false);
                    case ChessType.兵:
                        return new ChinChessBing(isRed, isJieQi: true, isBack: false);
                    case ChessType.車:
                        return new ChinChessJu(isRed, isJieQi: true, isBack: false);
                    case ChessType.馬:
                        return new ChinChessMa(isRed, isJieQi: true, isBack: false);
                    case ChessType.相:
                        return new ChinChessXiang(isRed, isJieQi: true, isBack: false);
                    case ChessType.仕:
                        return new ChinChessShi(isRed, isJieQi: true, isBack: false);
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        });

        this.RevokeCommand = new DelegateCommand(
            Revoke_CommandExecute,
            () => this.IsTurnToDo && CommandStack.Count > 0
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CommandStack.Count);

        this.SelectOrPutCommand = new DelegateCommand<ChinChessModel>(
            model =>
            {
                if (!this.SelectOrPutCommand_ExecuteCore(model))
                {
                    return;
                }

                var targetIsEmpty = model.Data.IsEmpty;
                // 选中
                bool canSelect = !model.Data.IsEmpty && model.Data.IsRed == this.IsRedTurn;
                if (canSelect)
                {
                    if (model.TrySelect(_preMoveVisitor))
                    {
                        CurrentChess = model;

                        this.Select_Mp3();

                        if (this.IsTurnToDo)
                        {
                            this.Log(this.Name, $"选中{new Position(model.Row, model.Column)}", this.IsRedRole == true);

                            _signalr.InvokeAsync("Move", new ChessInfo
                            {
                                FromRed = this.IsRedRole == true,
                                From = new Position(CurrentChess.Row, CurrentChess.Column),
                                To = new Position(model.Row, model.Column),
                            }.SerializeObject());
                        }
                    }

                    return;
                }

                // 移动棋子到这里 或 吃子
                if (this.CurrentChess.IsNotNullAnd(c => this.TryPutTo(c, new Position(model.Row, model.Column)))
                )
                {
                    if (this.IsTurnToDo)
                    {
                        var action = targetIsEmpty ? "移动" : "吃子";
                        this.Log(this.Name, $"{action}{new Position(this.CurrentChess.Row, this.CurrentChess.Column)}=>{new Position(model.Row, model.Column)}", this.IsRedRole == true);

                        _signalr.InvokeAsync("Move", new ChessInfo
                        {
                            FromRed = this.IsRedRole == true,
                            From = new Position(CurrentChess.Row, CurrentChess.Column),
                            To = new Position(model.Row, model.Column)
                        }.SerializeObject());
                    }

                    if (targetIsEmpty)
                    {
                        this.Go_Mp3();
                    }
                    else
                    {
                        this.Eat_Mp3();
                    }

                    this.From = this.CurrentChess;
                    this.To = model;

                    this.CurrentChess = null;

                    if (!this.CheckGameOver())
                    {
                        this.IsRedTurn = !IsRedTurn;
                    }
                }
            },
            model => this.IsTurnToDo && model != null && CurrentChess != model
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CurrentChess);
    }

    protected override void Timer_Tick(object sender, EventArgs e)
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

        base.Timer_Tick(sender, e);
    }

    #region 揭棋数据
    private IList<InnerChinChess> _black;
    private IList<InnerChinChess> _red;

    protected override void ReturnDataToJieQi(ChinChessModel chess)
    {
        base.ReturnDataToJieQi(chess);

        if (chess.Data.IsRed == true)
        {
            _red.Add(chess.Data);
        }
        else
        {
            _black.Add(chess.Data);
        }
    }

    public InnerChinChess GetNewChess(bool isRed)
    {
        AppUtils.Assert(_red.Count > 0 && _black.Count > 0, "没数据了");

        InnerChinChess? chess = default;

        if (isRed)
        {
            chess = _red.Last();

            _red.Remove(chess);
        }
        else
        {
            chess = _black.Last();

            _black.Remove(chess);
        }

        chess.OriginPos = new Position(-1, -1);

        return chess;
    }

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
                    this.Datas.Add(new ChinChessModel(row, column, true));
                }
            }
        });
    }
    #endregion

    public override bool TryPutTo(ChinChessModel chess, Position to)
    {
        if (this.TryPutToCore(chess, to))
        {
            if (chess.Data.IsBack == true)
            {
                chess.FlipChess(this.GetNewChess(chess.Row > 4));
            }

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

    protected override void DisposeCore()
    {
        if (!_black.IsNullOrEmpty())
        {
            foreach (var item in _black)
            {
                item.Dispose();
            }
            _black.Clear();
            _black = null;
        }

        if (!_red.IsNullOrEmpty())
        {
            foreach (var item in _red)
            {
                item.Dispose();
            }
            _red.Clear();
            _red = null;
        }

        base.DisposeCore();
    }
}
