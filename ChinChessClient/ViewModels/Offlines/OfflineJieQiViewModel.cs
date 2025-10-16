using ChinChessClient.Commands;
using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using IceTea.Wpf.Atom.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;

#pragma warning disable IDE0290 // 使用主构造函数
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
namespace ChinChessClient.ViewModels;

internal class OfflineJieQiViewModel : OfflineChinChessViewModelBase
{
    public override string Title => "本地版揭棋";

    public override ChinChessMode Mode => ChinChessMode.OfflineJieQi;

    public OfflineJieQiViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager) : base(appCfgHotKeyManager)
    {
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

                        this.Log(this.Name, $"选中{new Position(model.Row, model.Column)}", this.IsRedTurn == true);
                    }

                    return;
                }

                // 移动棋子到这里 或 吃子
                if (this.CurrentChess.IsNotNullAnd(c => this.TryPutTo(c, new Position(model.Row, model.Column)))
                )
                {
                    var action = targetIsEmpty ? "移动" : "吃子";
                    this.Log(this.Name, $"{action}{new Position(this.CurrentChess.Row, this.CurrentChess.Column)}=>{new Position(model.Row, model.Column)}", this.IsRedTurn == true);

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
            model => this.Status == GameStatus.Ready && model != null && CurrentChess != model
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CurrentChess);

        _black = GetRandSeq(false);
        _red = GetRandSeq(true);

        IList<InnerChinChess> GetRandSeq(bool isRed)
        {
            var seqs = new List<InnerChinChess>();

            var indexs = new List<ChessType>() { ChessType.兵, ChessType.炮, ChessType.車, ChessType.兵, ChessType.馬, ChessType.相, ChessType.兵, ChessType.仕, ChessType.炮, ChessType.兵, ChessType.車, ChessType.馬, ChessType.兵, ChessType.相, ChessType.仕 };
            var random = new Random();
            for (int i = 0; i < 15; i++)
            {
                var index = random.Next(0, indexs.Count);

                switch (indexs[index])
                {
                    case ChessType.兵:
                        seqs.Add(new ChinChessBing(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.炮:
                        seqs.Add(new ChinChessPao(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.車:
                        seqs.Add(new ChinChessJu(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.馬:
                        seqs.Add(new ChinChessMa(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.相:
                        seqs.Add(new ChinChessXiang(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.仕:
                        seqs.Add(new ChinChessShi(isRed, isJieQi: true, isBack: false));
                        break;
                    default:
                        break;
                }

                indexs.RemoveAt(index);
            }

            return seqs;
        }
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
