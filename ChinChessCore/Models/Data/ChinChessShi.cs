using ChinChessCore.Visitors;
using System;

namespace ChinChessCore.Models
{
    public class ChinChessShi : InnerChinChess
    {
        public ChinChessShi(bool isRed) : base(isRed, ChessType.仕) { }

        public ChinChessShi(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.仕, isJieQi, isBack) { }

        public override bool Accept(IVisitor visitor, Position to)
            => visitor.Visit(this, to);

        public override bool CanLeave(ICanPutToVisitor canPutToVisitor, bool? leaveInHorizontal = null)
        {
            Position from = this.CurPos;
            foreach (var item in new[] {
                                    new Position(from.Row - 1, from.Column - 1),
                                    new Position(from.Row - 1, from.Column + 1),
                                    new Position(from.Row + 1, from.Column - 1),
                                    new Position(from.Row + 1, from.Column + 1)
                                })
            {
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

            Position from = this.CurPos;
            var isAllow = Math.Abs(toPos.Row - from.Row) == 1 && Math.Abs(toPos.Column - from.Column) == 1;

            if (!isAllow)
            {
                return false;
            }

            return this.IsPosValid(toPos);
        }
    }
}