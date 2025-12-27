using ChinChessCore.Commands;
using ChinChessCore.Contracts;
using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;

#pragma warning disable IDE0290 // 使用主构造函数
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
namespace ChinChessClient.ViewModels;

internal class OfflineJieQiViewModel : OfflineChinChessViewModelBase
{
    public override ChinChessMode Mode => ChinChessMode.OfflineJieQi;

    public OfflineJieQiViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager)
        : base(appCfgHotKeyManager)
    {
        this.Status = EnumGameStatus.Ready;
    }

    protected override bool PushDead(InnerChinChess chess)
    {
        if (chess.IsEmpty)
        {
            return false;
        }

        if (chess.IsBack)
        {
            if (chess.IsRed == true)
            {
                chess = _red.Last();

                _red.Remove(chess);
            }
            else
            {
                chess = _black.Last();

                _black.Remove(chess);
            }

            chess.HasNotUsed = true;
        }

        base.PushDead(chess);

        return true;
    }

    protected override bool ReturnDead(InnerChinChess chess)
    {
        if (chess.IsEmpty)
        {
            return false;
        }

        if (chess.IsBack)
        {
            if (chess.IsRed == true)
            {
                chess = this.RedDeads.Last();

                _red.Add(chess);
            }
            else
            {
                chess = this.BlackDeads.Last();

                _black.Add(chess);
            }

            chess.HasNotUsed = false;
        }

        base.ReturnDead(chess);

        return true;
    }

    #region 揭棋数据
    private IList<InnerChinChess> _black;
    private IList<InnerChinChess> _red;

    protected override void TryReturnDataToJieQi(IChinChessCommand moveCommand)
    {
        base.TryReturnDataToJieQi(moveCommand);

        var dataIndex = moveCommand.To.Index;

        var data = this.Datas[dataIndex].Data;

        bool hasReturnToOrigin = data.IsJieQi && data.OriginPos == moveCommand.From;

        if (hasReturnToOrigin)
        {
            if (data.IsRed == true)
            {
                _red.Add(data);
            }
            else
            {
                _black.Add(data);
            }
        }
    }

    public InnerChinChess GetNewChess(bool isRed)
    {
        InnerChinChess? chess = default;

        if (isRed)
        {
            AppUtils.Assert(_red.Count > 0, "红方没数据了");

            chess = _red.Last();

            _red.Remove(chess);
        }
        else
        {
            AppUtils.Assert(_black.Count > 0, "黑方没数据了");

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

        this.Datas.Clear();

        for (int row = 0; row < 10; row++)
        {
            for (int column = 0; column < 9; column++)
            {
                this.Datas.Add(new ChinChessModel(row, column, true));
            }
        }
    }

    #endregion

    private void InitJieQiData()
    {
        _black = GetRandSeq(false);
        _red = GetRandSeq(true);

        IList<InnerChinChess> GetRandSeq(bool isRed)
        {
            var seq = new List<InnerChinChess>();

            var indexs = new List<ChessType>() { ChessType.兵, ChessType.炮, ChessType.車, ChessType.兵, ChessType.馬, ChessType.相, ChessType.兵, ChessType.仕, ChessType.炮, ChessType.兵, ChessType.車, ChessType.馬, ChessType.兵, ChessType.相, ChessType.仕 };
            var random = new Random();
            for (int i = 0; i < 15; i++)
            {
                var index = random.Next(0, indexs.Count);

                switch (indexs[index])
                {
                    case ChessType.兵:
                        seq.Add(new ChinChessBing(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.炮:
                        seq.Add(new ChinChessPao(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.車:
                        seq.Add(new ChinChessJu(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.馬:
                        seq.Add(new ChinChessMa(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.相:
                        seq.Add(new ChinChessXiang(isRed, isJieQi: true, isBack: false));
                        break;
                    case ChessType.仕:
                        seq.Add(new ChinChessShi(isRed, isJieQi: true, isBack: false));
                        break;
                    default:
                        break;
                }

                indexs.RemoveAt(index);
            }

            return seq;
        }
    }

    protected override void OnGameStatusChanged(EnumGameStatus newStatus)
    {
        base.OnGameStatusChanged(newStatus);

        switch (newStatus)
        {
            case EnumGameStatus.Ready:
                this.InitJieQiData();
                break;
            case EnumGameStatus.Stoped:
                foreach (var item in this.Datas.Where(d => d.Data.IsBack))
                {
                    item.Data.HasNotUsed = true;

                    item.FlipChess(this.GetNewChess(item.Data.IsRed == true));
                }
                break;
            default:
                break;
        }
    }

    protected override void PreMove(ChinChessModel chess)
    {
        if (chess.Data.IsBack == true)
        {
            chess.FlipChess(this.GetNewChess(chess.Row > 4));
        }
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
