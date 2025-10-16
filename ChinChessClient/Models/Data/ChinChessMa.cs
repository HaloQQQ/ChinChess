using ChinChessClient.Visitors;
using ChinChessCore.Models;
using IceTea.Pure.Utils;

namespace ChinChessClient.Models;

/// <summary>
/// 马走日
/// </summary>
internal class ChinChessMa : InnerChinChess
{
    public ChinChessMa(bool isRed) : base(isRed, ChessType.馬) { }

    public ChinChessMa(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.馬, isJieQi, isBack) { }

    public override bool Accept(IVisitor visitor, Position from, Position to)
        => visitor.Visit(this, from, to);

    public override bool CanLeave(IVisitor canEatVisitor, Position from, bool _ = true)
    {
        foreach (var item in new[] {
                                new Position(from.Row - 2, from.Column - 1),
                                new Position(from.Row - 1, from.Column - 2),
                                new Position(from.Row - 2, from.Column + 1),
                                new Position(from.Row - 1, from.Column + 2),
                                new Position(from.Row + 2, from.Column - 1),
                                new Position(from.Row + 1, from.Column - 2),
                                new Position(from.Row + 2, from.Column + 1),
                                new Position(from.Row + 1, from.Column + 2)
                            })
        {
            if (this.CanPutTo(canEatVisitor, from, item))
            {
                return true;
            }
        }

        return false;
    }

    public Position GetMaBarrier(Position from, Position to)
    {
        var isHorizontal = Math.Abs(to.Row - from.Row) == 1 && Math.Abs(to.Column - from.Column) == 2;

        var isVertical = Math.Abs(to.Row - from.Row) == 2 && Math.Abs(to.Column - from.Column) == 1;

        AppUtils.AssertDataValidation(isHorizontal || isVertical, "马的数据不对");

        if (isHorizontal)
        {
            return new Position(from.Row, (from.Column + to.Column) / 2);
        }
        else
        {
            return new Position((from.Row + to.Row) / 2, from.Column);
        }
    }
}
