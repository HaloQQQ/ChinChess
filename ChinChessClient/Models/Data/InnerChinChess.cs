using ChinChessClient.Visitors;
using ChinChessCore.Models;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Extensions;
using ImTools;
using System.Diagnostics;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessClient.Models;

[DebuggerDisplay("IsRed={IsRed}, Type={Type}")]
internal class InnerChinChess : CloneableBase
{
    public static InnerChinChess Empty = new();

    private InnerChinChess() { }

    public InnerChinChess(bool isRed, ChessType type, bool isJieQi = false, bool isBack = false)
    {
        IsRed = isRed;
        Type = type;

        IsBack = isBack;
        IsJieQi = isJieQi;
    }

    public bool IsBack { get; }
    public bool IsJieQi { get; }

    public ChessType? Type { get; }
    public bool IsEmpty => Type == null;

    public bool? IsRed { get; }

    private Position _originPos = new Position(-1, -1);
    internal Position OriginPos
    {
        get => _originPos; 
        set => _originPos = value;
    }

    #region IChineseChess
    public bool CanPutTo(IVisitor canEatVisitor, Position from, Position to)
        => this.Accept(canEatVisitor, from, to);

    public bool PreMove(IVisitor preMoveVisitor, Position from)
        => this.Accept(preMoveVisitor, from, default);

    public bool CanBeProtected(IVisitor notFatalVisitor, Position killer, Position to)
        => this.Accept(notFatalVisitor, killer, to);

    public bool IsDangerous(IVisitor canEatVisitor, Position current, out ChinChessModel killer)
    {
        int toRow = current.Row, toColumn = current.Column;

        #region 兵
        foreach (var item in new[] {
                                new Position(toRow - 1, toColumn),
                                new Position(toRow + 1, toColumn),
                                new Position(toRow, toColumn - 1),
                                new Position(toRow, toColumn + 1)
                            })
        {
            if (CanEnemyTo(canEatVisitor, item, current, ChessType.兵))
            {
                killer = canEatVisitor.GetChess(item.Row, item.Column);

                return true;
            }
        }
        #endregion

        #region 马
        foreach (var item in new[] {
                                new Position(toRow - 2, toColumn - 1),
                                new Position(toRow - 1, toColumn - 2),
                                new Position(toRow - 2, toColumn + 1),
                                new Position(toRow - 1, toColumn + 2),
                                new Position(toRow + 2, toColumn - 1),
                                new Position(toRow + 1, toColumn - 2),
                                new Position(toRow + 2, toColumn + 1),
                                new Position(toRow + 1, toColumn + 2)
                            })
        {
            if (CanEnemyTo(canEatVisitor, item, current, ChessType.馬))
            {
                killer = canEatVisitor.GetChess(item.Row, item.Column);

                return true;
            }
        }
        #endregion

        #region 車
        foreach ((int rowStep, int columnStep) in new Tuple<int, int>[] {
                                    new(-1,0),
                                    new(1,0),
                                    new(0,-1),
                                    new(0,1)
                                }
                )
        {
            if (FindJu(canEatVisitor, toRow, toColumn, rowStep, columnStep, out Position pos))
            {
                killer = canEatVisitor.GetChess(pos.Row, pos.Column);

                return true;
            }
        }

        bool FindJu(IVisitor canEatVisitor, int row, int column, int rowStep, int columnStep, out Position pos)
        {
            int fromRow = row + rowStep, fromColumn = column + columnStep;

            while (fromRow.IsInRange(0, 9) && fromColumn.IsInRange(0, 8))
            {
                if (!canEatVisitor.GetChessData(fromRow, fromColumn).IsEmpty)
                {
                    var point = new Position(fromRow, fromColumn);
                    if (IsEnemy(canEatVisitor, point, ChessType.車))
                    {
                        pos = point;
                        return true;
                    }

                    break;
                }

                fromRow += rowStep;
                fromColumn += columnStep;
            }

            pos = default;

            return false;
        }
        #endregion

        #region 炮
        foreach ((int rowStep, int columnStep) in new Tuple<int, int>[] {
                                    new(-1, 0),
                                    new(1, 0),
                                    new(0, -1),
                                    new(0, 1)
                                }
                )
        {
            if (FindPao(canEatVisitor, toRow, toColumn, rowStep, columnStep, out Position pos))
            {
                killer = canEatVisitor.GetChess(pos.Row, pos.Column);

                return true;
            }
        }

        bool FindPao(IVisitor visitor, int row, int column, int rowStep, int columnStep, out Position pos)
        {
            int fromRow = row + rowStep, fromColumn = column + columnStep;

            int mountainsCount = 0;

            while (fromRow.IsInRange(0, 9) && fromColumn.IsInRange(0, 8))
            {
                if (!visitor.GetChessData(fromRow, fromColumn).IsEmpty)
                {
                    mountainsCount++;

                    if (mountainsCount == 2)
                    {
                        var point = new Position(fromRow, fromColumn);
                        if (IsEnemy(visitor, point, ChessType.炮))
                        {
                            pos = point;
                            return true;
                        }

                        break;
                    }
                }

                fromRow += rowStep;
                fromColumn += columnStep;
            }

            pos = default;

            return false;
        }
        #endregion

        #region 相
        foreach (var item in new[] {
                                new Position(toRow - 2, toColumn - 2),
                                new Position(toRow - 2, toColumn + 2),
                                new Position(toRow + 2, toColumn - 2),
                                new Position(toRow + 2, toColumn + 2)
                            })
        {
            if (CanEnemyTo(canEatVisitor, item, current, ChessType.相))
            {
                killer = canEatVisitor.GetChess(item.Row, item.Column);

                return true;
            }
        }
        #endregion

        #region 仕
        foreach (var item in new[] {
                                new Position(toRow - 1, toColumn - 1),
                                new Position(toRow - 1, toColumn + 1),
                                new Position(toRow + 1, toColumn - 1),
                                new Position(toRow + 1, toColumn + 1)
                            })
        {
            if (CanEnemyTo(canEatVisitor, item, current, ChessType.仕))
            {
                killer = canEatVisitor.GetChess(item.Row, item.Column);

                return true;
            }
        }
        #endregion

        #region 帥
        foreach (var item in new[] {
                                new Position(toRow, toColumn - 1),
                                new Position(toRow, toColumn + 1),
                                new Position(toRow - 1, toColumn),
                                new Position(toRow + 1, toColumn)
                            })
        {
            if (CanEnemyTo(canEatVisitor, item, current, ChessType.帥))
            {
                killer = canEatVisitor.GetChess(item.Row, item.Column);

                return true;
            }
        }
        #endregion

        killer = default;

        return false;
    }
    #endregion

    public bool FaceToFace(IVisitor visitor)
    {
        var shuais = visitor.GetChesses()
                .Where(c => c.Data.Type == ChessType.帥);

        var redShuai = shuais.First(c => c.Data.IsRed == true);
        var blackShuai = shuais.First(c => c.Data.IsRed == false);

        // 王见王
        if (redShuai.Column == blackShuai.Column)
        {
            int fromRow = Math.Min(redShuai.Row, blackShuai.Row);
            int toRow = Math.Max(redShuai.Row, blackShuai.Row);

            var currentRow = fromRow + 1;
            while (currentRow < toRow)
            {
                var currentData = visitor.GetChessData(currentRow, blackShuai.Column);

                if (!currentData.IsEmpty)
                {
                    return false;
                }

                currentRow++;
            }

            return true;
        }

        return false;
    }

    public virtual bool CanLeave(IVisitor canEatVisitor, Position from, bool isHorizontal = true)
        => throw new NotImplementedException();

    /// <summary>
    /// 起点位置是否为敌方<see cref="chessType"/>, 并且可以放置到目标点
    /// </summary>
    /// <param name="canEatVisitor"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="chessType"></param>
    /// <returns></returns>
    public bool CanEnemyTo(IVisitor canEatVisitor, Position from, Position to, ChessType chessType)
    {
        if (IsEnemy(canEatVisitor, from, chessType))
        {
            var data = canEatVisitor.GetChessData(from.Row, from.Column);

            if (data.CanPutTo(canEatVisitor, from, to))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 当前点是否为敌方<see cref="chessType"/>中的一个
    /// </summary>
    /// <param name="visitor"></param>
    /// <param name="position"></param>
    /// <param name="chessType"></param>
    /// <returns></returns>
    public bool IsEnemy(IVisitor visitor, Position position, ChessType chessType)
    {
        if (!IsPosValid_Abs(chessType, position, true))
        {
            return false;
        }

        var data = visitor.GetChessData(position.Row, position.Column);

        return this.IsEnemy(data) && ((data.Type & chessType) == data.Type);
    }

    /// <summary>
    /// 是否为敌军
    /// </summary>
    /// <param name="chinChess"></param>
    /// <returns></returns>
    public bool IsEnemy(InnerChinChess chinChess)
    {
        //AppUtils.AssertOperationValidation(!this.IsEmpty, "非法操作");

        return !chinChess.IsEmpty && this.IsRed != chinChess.IsRed;
    }

    public bool IsPosValid(Position pos)
        => this.IsPosValid_Abs((ChessType)this.Type, pos, false);

    public bool IsPoseValid_Rel(ChessType chessType, Position pos, bool isEnemy = true)
    {
        if (chessType != ChessType.帥 && this.IsJieQi)
        {
            return pos.Row.IsInRange(0, 9) && pos.Column.IsInRange(0, 8);
        }

        Predicate<Position> predicate = null;

        switch (chessType)
        {
            case ChessType.兵:
                if (this.IsRed == isEnemy)
                {
                    predicate = p => p.Row.IsInRange(3, 9) && p.Column.IsInRange(0, 8);
                }
                else
                {
                    predicate = p => p.Row.IsInRange(0, 6) && p.Column.IsInRange(0, 8);
                }
                break;
            case ChessType.相:
                if (this.IsRed == isEnemy)
                {
                    predicate = p => p.Row.IsInRange(0, 4) && p.Column.IsInRange(0, 8);
                }
                else
                {
                    predicate = p => p.Row.IsInRange(5, 9) && p.Column.IsInRange(0, 8);
                }
                break;
            case ChessType.仕:
            case ChessType.帥:
                if (this.IsRed == isEnemy)
                {
                    predicate = p => p.Row.IsInRange(0, 2) && p.Column.IsInRange(3, 5);
                }
                else
                {
                    predicate = p => p.Row.IsInRange(7, 9) && p.Column.IsInRange(3, 5);
                }
                break;
            default:
                predicate = p => p.Row.IsInRange(0, 9) && p.Column.IsInRange(0, 8);
                break;
        }

        return predicate(pos);
    }

    public bool IsPosValid_Abs(ChessType chessType, Position pos, bool isEnemy = true)
    {
        if (chessType != ChessType.帥 && this.IsJieQi)
        {
            return pos.Row.IsInRange(0, 9) && pos.Column.IsInRange(0, 8);
        }

        Predicate<Position> predicate = null;

        switch (chessType)
        {
            case ChessType.兵:
                if (this.IsRed == isEnemy)
                {
                    predicate = p => p.Row.IsInRange(5, 9) && p.Column.IsInRange(0, 8)
                                    || p.IsIn(new[] {
                                                new Position(3, 0), new Position(3, 2),
                                                new Position(3, 4), new Position(3, 6),
                                                new Position(3, 8),
                                                new Position(4, 0), new Position(4, 2),
                                                new Position(4, 4), new Position(4, 6),
                                                new Position(4, 8) });
                }
                else
                {
                    predicate = p => p.Row.IsInRange(0, 4) && p.Column.IsInRange(0, 8)
                                     || p.IsIn(new[] {
                                                new Position(6, 0), new Position(6, 2),
                                                new Position(6, 4), new Position(6, 6),
                                                new Position(6, 8),
                                                new Position(5, 0), new Position(5, 2),
                                                new Position(5, 4), new Position(5, 6),
                                                new Position(5, 8) });
                }
                break;
            case ChessType.相:
                if (this.IsRed == isEnemy)
                {
                    predicate = p => p.IsIn(new[] {
                                            new Position(0, 2), new Position(0, 6),
                                            new Position(2, 0), new Position(2, 4), new Position(2, 8),
                                            new Position(4, 2), new Position(4, 6)});
                }
                else
                {
                    predicate = p => p.IsIn(new[] {
                                            new Position(9, 2), new Position(9, 6),
                                            new Position(7, 0), new Position(7, 4), new Position(7, 8),
                                            new Position(5, 2), new Position(5, 6)});
                }
                break;
            case ChessType.仕:
                if (this.IsRed == isEnemy)
                {
                    predicate = p => p.IsIn(new[] {
                                            new Position(0, 3), new Position(0, 5),
                                            new Position(1, 4),
                                            new Position(2, 3), new Position(2, 5)});
                }
                else
                {
                    predicate = p => p.IsIn(new[] {
                                            new Position(9, 3), new Position(9, 5),
                                            new Position(8, 4),
                                            new Position(7, 3), new Position(7, 5)});
                }
                break;
            case ChessType.帥:
                if (this.IsRed == isEnemy)
                {
                    predicate = p => p.Row.IsInRange(0, 2) && p.Column.IsInRange(3, 5);
                }
                else
                {
                    predicate = p => p.Row.IsInRange(7, 9) && p.Column.IsInRange(3, 5);
                }
                break;
            default:
                predicate = p => p.Row.IsInRange(0, 9) && p.Column.IsInRange(0, 8);
                break;
        }

        return predicate(pos);
    }

    public virtual bool Accept(IVisitor visitor, Position from, Position to) => throw new NotImplementedException();
}
