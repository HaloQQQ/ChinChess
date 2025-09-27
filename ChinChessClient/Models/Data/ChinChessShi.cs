using ChinChessClient.Visitors;
using ChinChessCore.Models;

namespace ChinChessClient.Models;

internal class ChineseChessShi : InnerChinChess
{
    public ChineseChessShi(bool isRed) : base(isRed, ChessType.仕) { }

    public override bool Accept(IVisitor visitor, Position from, Position to)
        => visitor.Visit(this, from, to);

    public override bool CanLeave(IVisitor canEatVisitor, Position from, bool _ = true)
    {
        foreach (var item in new[] {
                                    new Position(from.Row - 1, from.Column - 1),
                                    new Position(from.Row - 1, from.Column + 1),
                                    new Position(from.Row + 1, from.Column - 1),
                                    new Position(from.Row + 1, from.Column - 1)
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
