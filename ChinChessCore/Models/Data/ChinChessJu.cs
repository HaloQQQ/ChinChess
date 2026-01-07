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

        public override bool Accept(IVisitor visitor, Position to)
            => visitor.Visit(this, to);

        public override bool CanLeave(ICanPutToVisitor canPutToVisitor, bool? leaveInHorizontal = null)
        {
            Position from = this.CurPos;
            foreach (var item in new[] {
                                    new Position(from.Row + 1, from.Column),
                                    new Position(from.Row - 1, from.Column),
                                    new Position(from.Row, from.Column + 1),
                                    new Position(from.Row, from.Column - 1)
                                })
            {
                if (leaveInHorizontal != null)
                {
                    if (leaveInHorizontal == true)
                    {
                        if (this.CurPos.Column == item.Column)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (this.CurPos.Row == item.Row)
                        {
                            continue;
                        }
                    }
                }

                if (this.CanPutTo(canPutToVisitor, item))
                {
                    return true;
                }
            }

            return false;
        }


        internal override bool IsAllowTo(Position toPos)
        {
            if (!base.IsAllowTo(toPos))
            {
                return false;
            }

            return this.CurPos != toPos && (this.CurPos.Row == toPos.Row || this.CurPos.Column == toPos.Column);
        }
    }
}