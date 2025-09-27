using ChinChessClient.Visitors;
using ChinChessCore.Models;

namespace ChinChessClient.Models;

internal class ChinChessShuai : InnerChinChess
{
    public ChinChessShuai(bool isRed) : base(isRed, ChessType.帥) { }

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
            if (this.CanPutTo(canEatVisitor, from, item))
            {
                return true;
            }
        }

        return false;
    }
}
