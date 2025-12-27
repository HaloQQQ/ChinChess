using ChinChessCore.Visitors;

namespace ChinChessCore.Models
{
    /// <summary>
    /// 只能水平和垂直移动
    /// </summary>
    public class ChinChessJu : InnerChinChess
    {
        public ChinChessJu(bool isRed) : base(isRed, ChessType.車) { }

        public ChinChessJu(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.車, isJieQi, isBack) { }

        public override bool Accept(IVisitor visitor, Position from, Position to)
            => visitor.Visit(this, from, to);

        public override bool CanLeave(ICanPutToVisitor canPutToVisitor, Position from, bool isHorizontal = true)
        {
            var rowStep = isHorizontal ? 1 : 0;
            var columnStep = isHorizontal ? 0 : 1;

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