using ChinChessCore.Visitors;

namespace ChinChessCore.Models
{
    /// <summary>
    /// 过河前只能前进，过河后可以左右前
    /// </summary>
    public class ChinChessBing : InnerChinChess
    {
        public ChinChessBing(bool isRed) : base(isRed, ChessType.兵) { }

        public ChinChessBing(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.兵, isJieQi, isBack) { }

        public override bool Accept(IVisitor visitor, Position from, Position to)
            => visitor.Visit(this, from, to);

        public override bool CanLeave(ICanPutToVisitor canPutToVisitor, Position from, bool isHorizontal = true)
        {
            var rowStep = isHorizontal ? 1 : 0;
            var columnStep = isHorizontal == false ? 1 : 0;

            foreach (var item in new[] {
                                    new Position(from.Row + rowStep, from.Column + columnStep),
                                    new Position(from.Row - rowStep, from.Column - columnStep)
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
