using ChinChessCore.Visitors;

namespace ChinChessCore.Models
{
    public class ChinChessShi : InnerChinChess
    {
        public ChinChessShi(bool isRed) : base(isRed, ChessType.仕) { }

        public ChinChessShi(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.仕, isJieQi, isBack) { }

        public override bool Accept(IVisitor visitor, Position from, Position to)
            => visitor.Visit(this, from, to);

        public override bool CanLeave(ICanPutToVisitor canPutToVisitor, Position from, bool _ = true)
        {
            foreach (var item in new[] {
                                    new Position(from.Row - 1, from.Column - 1),
                                    new Position(from.Row - 1, from.Column + 1),
                                    new Position(from.Row + 1, from.Column - 1),
                                    new Position(from.Row + 1, from.Column - 1)
                                })
            {
                if (this.CanPutTo(canPutToVisitor, from, item))
                {
                    return true;
                }
            }

            return false;
        }
    }
}