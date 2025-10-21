using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Pure.Utils;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessClient.Visitors;

internal interface ICanPutToVisitor : IVisitor { }

/// <summary>
/// 检测吃子或移动
/// </summary>
internal class CanPutVisitor : VisitorBase, ICanPutToVisitor
{
    public CanPutVisitor(IList<ChinChessModel> datas) : base(datas) { }

    public override bool Visit(ChinChessJu chess, Position from, Position to)
    {
        if (!this.TryMoveCore(chess, from, to))
        {
            return false;
        }

        int fromRow = from.Row, fromColumn = from.Column;
        int toRow = to.Row, toColumn = to.Column;

        var isSameRow = fromRow == toRow;
        var isSameColumn = fromColumn == toColumn;

        if (!isSameRow && !isSameColumn)
        {
            return false;
        }

        int rowStep = isSameRow ? 0 : (toRow > fromRow ? 1 : -1);
        int columnStep = isSameColumn ? 0 : (toColumn > fromColumn ? 1 : -1);

        int currentRow = fromRow + rowStep, currentColumn = fromColumn + columnStep;

        var currentPos = new Position(currentRow, currentColumn);
        while (currentPos != to)
        {
            if (!this.GetChessData(currentRow, currentColumn).IsEmpty)
            {
                return false;
            }

            currentPos = new Position(
                currentRow += rowStep,
                currentColumn += columnStep);
        }

        return true;
    }

    public override bool Visit(ChinChessPao chess, Position from, Position to)
    {
        if (!this.TryMoveCore(chess, from, to))
        {
            return false;
        }

        int fromRow = from.Row, fromColumn = from.Column;
        int toRow = to.Row, toColumn = to.Column;

        // 移动
        var isSameRow = fromRow == toRow;
        var isSameColumn = fromColumn == toColumn;

        if (!isSameRow && !isSameColumn)
        {
            return false;
        }

        int rowStep = isSameRow ? 0 : (toRow > fromRow ? 1 : -1);
        int columnStep = isSameColumn ? 0 : (toColumn > fromColumn ? 1 : -1);

        int currentRow = fromRow + rowStep, currentColumn = fromColumn + columnStep;

        var mountainsCount = 0;
        var current = new Position(currentRow, currentColumn);
        while (current != to)
        {
            if (!this.GetChessData(currentRow, currentColumn).IsEmpty)
            {
                // 吃空子
                if (this.GetChessData(toRow, toColumn).IsEmpty)
                {
                    return false;
                }

                // 超过一个支点
                if (++mountainsCount == 2)
                {
                    return false;
                }
            }

            current = new Position(
                currentRow += rowStep,
                currentColumn += columnStep);
        }

        return true;
    }

    public override bool Visit(ChinChessBing chess, Position from, Position to)
    {
        if (!this.TryMoveCore(chess, from, to))
        {
            return false;
        }

        int fromRow = from.Row, fromColumn = from.Column;
        int toRow = to.Row, toColumn = to.Column;

        if (Math.Abs(fromRow - toRow) + Math.Abs(fromColumn - toColumn) != 1)
        {
            return false;
        }

        if (!chess.IsPosValid(from) || !chess.IsPosValid(to))
        {
            return false;
        }

        if (chess.IsRed == true)
        {
            if (fromRow - 1 == toRow) // 前进
            {
                return true;
            }

            if (fromRow < 5) // 过河后左右
            {
                if (fromRow == toRow)
                {
                    return true;
                }
            }
        }
        else
        {
            if (fromRow + 1 == toRow) // 前进
            {
                return true;
            }

            if (fromRow > 4) // 过河后左右
            {
                if (fromRow == toRow)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override bool Visit(ChinChessMa chess, Position from, Position to)
    {
        if (!this.TryMoveCore(chess, from, to))
        {
            return false;
        }

        var isHorizontal = Math.Abs(from.Row - to.Row) == 1 && Math.Abs(from.Column - to.Column) == 2;
        var isVertical = Math.Abs(from.Row - to.Row) == 2 && Math.Abs(from.Column - to.Column) == 1;

        if (!isHorizontal && !isVertical)
        {
            return false;
        }

        var barrierPos = chess.GetMaBarrier(from, to);

        return this.GetChessData(barrierPos.Row, barrierPos.Column).IsEmpty;
    }

    public override bool Visit(ChinChessXiang chess, Position from, Position to)
    {
        if (!this.TryMoveCore(chess, from, to))
        {
            return false;
        }

        if (Math.Abs(to.Row - from.Row) != 2 || Math.Abs(to.Column - from.Column) != 2)
        {
            return false;
        }

        if (!chess.IsPosValid(from) || !chess.IsPosValid(to))
        {
            return false;
        }

        var barrierPos = chess.GetXiangBarrier(from, to);

        return this.GetChessData(barrierPos.Row, barrierPos.Column).IsEmpty;
    }

    public override bool Visit(ChinChessShi chess, Position from, Position to)
    {
        if (!this.TryMoveCore(chess, from, to))
        {
            return false;
        }

        if (!chess.IsPosValid(from) || !chess.IsPosValid(to))
        {
            return false;
        }

        return Math.Abs(to.Row - from.Row) == 1 && Math.Abs(to.Column - from.Column) == 1;
    }

    public override bool Visit(ChinChessShuai chess, Position from, Position to)
    {
        if (!this.TryMoveCore(chess, from, to))
        {
            return false;
        }

        if (!chess.IsPosValid(from) || !chess.IsPosValid(to))
        {
            return false;
        }

        return Math.Abs(to.Row - from.Row) + Math.Abs(to.Column - from.Column) == 1;
    }

    /// <summary>
    /// 起点到终点必须不同
    /// 检测起点到终点的颜色及状态
    /// </summary>
    /// <param name="chess"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    protected override bool TryMoveCore(InnerChinChess chess, Position from, Position to)
    {
        if (!base.TryMoveCore(chess, from, to))
        {
            return false;
        }

        if (from == to)
        {
            return false;
        }

        var fromData = this.GetChessData(from.Row, from.Column);

        AppUtils.AssertDataValidation(fromData == chess, $"{chess.Type}不在{from.ToString()}");

        int toRow = to.Row, toColumn = to.Column;
        var targetData = this.GetChessData(toRow, toColumn);

        bool prevent = fromData.IsEmpty
                        || (!targetData.IsEmpty && fromData.IsRed == targetData.IsRed);

        return !prevent;
    }
}
