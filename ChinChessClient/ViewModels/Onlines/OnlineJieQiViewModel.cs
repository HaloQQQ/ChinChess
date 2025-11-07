using ChinChessClient.Commands;
using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessCore.Models;
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
            this.Log(this.Name, "收到揭棋序列", this.IsRedRole == true);

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
            SelectOrPut_CommandExecute,
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

    protected override void OnGameStatusChanged(GameStatus newStatus)
    {
        base.OnGameStatusChanged(newStatus);

        switch (newStatus)
        {
            case GameStatus.Stoped:
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
