using ChinChessCore.Commands;
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

            return TryLeave(rowStep, columnStep) || TryLeave(-rowStep, -columnStep);

            bool TryLeave(int __rowStep, int __columnStep)
            {
                Position currentPos = new Position(from.Row + __rowStep, from.Column + __columnStep);

                while (currentPos.IsValid)
                {
                    if (!canPutToVisitor.GetChessData(currentPos).IsEmpty)
                    {
                        break;
                    }

                    using (new MockMoveCommand(canPutToVisitor.GetChess(from), canPutToVisitor.GetChess(currentPos))
                                .Execute()
                          )
                    {
                        if (!this.IsDangerous(canPutToVisitor, currentPos, out _))
                        {
                            return true;
                        }
                    }

                    currentPos = new Position(from.Row + __rowStep, from.Column + __columnStep);
                }

                return false;
            }
        }
    }
}