using ChinChessClient.Visitors;
using ChinChessCore.Models;

namespace ChinChessClient.Models;

/// <summary>
/// 只能水平和垂直移动
/// </summary>
internal class ChinChessJu : InnerChinChess
{
    public ChinChessJu(bool isRed) : base(isRed, ChessType.車) { }

    public override bool Accept(IVisitor visitor, Position from, Position to)
        => visitor.Visit(this, from, to);

    public override bool CanLeave(IVisitor canEatVisitor, Position from, bool isHorizontal = true)
    {
        var rowStep = isHorizontal ? 1 : 0;
        var columnStep = isHorizontal ? 0 : 1;

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
