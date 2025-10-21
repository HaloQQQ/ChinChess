using ChinChessClient.Visitors;
using ChinChessCore.Models;
using IceTea.Pure.Utils;

namespace ChinChessClient.Models;

internal class ChinChessXiang : InnerChinChess
{
    public ChinChessXiang(bool isRed) : base(isRed, ChessType.相) { }

    public ChinChessXiang(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.相, isJieQi, isBack) { }

    public override bool Accept(IVisitor visitor, Position from, Position to)
        => visitor.Visit(this, from, to);

    public override bool CanLeave(ICanPutToVisitor canPutToVisitor, Position from, bool _ = true)
    {
        foreach (var item in new[] {
                                    new Position(from.Row - 2, from.Column - 2),
                                    new Position(from.Row - 2, from.Column + 2),
                                    new Position(from.Row + 2, from.Column - 2),
                                    new Position(from.Row + 2, from.Column - 2)
                                })
        {
            if (this.CanPutTo(canPutToVisitor, from, item))
            {
                return true;
            }
        }

        return false;
    }

    public Position GetXiangBarrier(Position from, Position to)
    {
        AppUtils.AssertDataValidation(
            Math.Abs(to.Row - from.Row) == 2 && Math.Abs(to.Column - from.Column) == 2,
            "象的数据不对");

        return new Position((from.Row + to.Row) / 2 , (from.Column + to.Column) / 2);
    }
}
