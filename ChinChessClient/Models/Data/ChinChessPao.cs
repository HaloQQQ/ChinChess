using ChinChessClient.Visitors;
using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;

namespace ChinChessClient.Models;

/// <summary>
/// 隔山打牛
/// </summary>
internal class ChinChessPao : InnerChinChess
{
    public ChinChessPao(bool isRed) : base(isRed, ChessType.炮) { }

    public override bool Accept(IVisitor visitor, Position from, Position to)
        => visitor.Visit(this, from, to);

    public override bool CanLeave(IVisitor canEatVisitor, Position from, bool isHorizontal = true)
    {
        var rowStep = isHorizontal ? 1 : 0;
        var columnStep = isHorizontal == false ? 1 : 0;

        foreach (var item in new[] {
                                    new Position(from.Row + rowStep, from.Column + columnStep),
                                    new Position(from.Row - rowStep, from.Column - columnStep)
                                })
        {
            if (this.IsPosValid_Abs(ChessType.炮, item, false)
                && canEatVisitor.GetChessData(item.Row, item.Column).IsEmpty
                )
            {
                return true;
            }
        }

        int currentRow = from.Row + rowStep, currentColumn = from.Column + columnStep;

        int mountainsCount = 0;

        while (currentRow.IsInRange(0, 9) && currentColumn.IsInRange(0, 8))
        {
            InnerChinChess current = canEatVisitor.GetChessData(currentRow, currentColumn);
            if (!current.IsEmpty)
            {
                mountainsCount++;

                if (mountainsCount == 2)
                {
                    if (this.IsEnemy(current))
                    {
                        return true;
                    }

                    return false;
                }
            }

            currentRow += rowStep;
            currentColumn += columnStep;
        }

        return false;
    }

    public Position GetPaoBarrier(IVisitor visitor, Position from, Position to)
    {
        AppUtils.AssertDataValidation(visitor.GetChessData(from.Row, from.Column) == this, $"{this.Type}应该在{from.ToString()}");

        int rowStep = (to.Row == from.Row) ? 0 : (to.Row > from.Row ? 1 : -1);
        int columnStep = (to.Column == from.Column) ? 0 : (to.Column > from.Column ? 1 : -1);

        int currentRow = from.Row + rowStep, currentColumn = from.Column + columnStep;
        var pos = new Position(currentRow, currentColumn);

        while (pos != to)
        {
            if (!visitor.GetChessData(currentRow, currentColumn).IsEmpty)
            {
                return pos;
            }

            pos = new Position(currentRow += rowStep, currentColumn += columnStep);
        }

        throw new InvalidOperationException();
    }
}
