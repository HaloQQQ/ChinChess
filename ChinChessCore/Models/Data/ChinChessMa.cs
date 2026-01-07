using ChinChessCore.Visitors;
using IceTea.Pure.Utils;
using System;

namespace ChinChessCore.Models
{
    /// <summary>
    /// 马走日
    /// </summary>
    public class ChinChessMa : InnerChinChess
    {
        public ChinChessMa(bool isRed) : base(isRed, ChessType.馬) { }

        public ChinChessMa(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.馬, isJieQi, isBack) { }

        public override bool Accept(IVisitor visitor, Position to)
            => visitor.Visit(this, to);

        public override bool CanLeave(ICanPutToVisitor canPutToVisitor, bool? leaveInHorizontal = null)
        {
            Position from = this.CurPos;
            foreach (var item in new[] {
                                new Position(from.Row - 2, from.Column - 1),
                                new Position(from.Row - 1, from.Column - 2),
                                new Position(from.Row - 2, from.Column + 1),
                                new Position(from.Row - 1, from.Column + 2),
                                new Position(from.Row + 2, from.Column - 1),
                                new Position(from.Row + 1, from.Column - 2),
                                new Position(from.Row + 2, from.Column + 1),
                                new Position(from.Row + 1, from.Column + 2)
                            })
            {
                if (this.CanPutTo(canPutToVisitor, item))
                {
                    return true; 
                }
            }

            return false;
        }

        public bool TryGetMaBarrier(IVisitor visitor, Position to, out Position maBarrierPos)
        {
            Position from = this.CurPos;
            var isHorizontal = Math.Abs(to.Row - from.Row) == 1 && Math.Abs(to.Column - from.Column) == 2;

            var isVertical = Math.Abs(to.Row - from.Row) == 2 && Math.Abs(to.Column - from.Column) == 1;

            AppUtils.AssertDataValidation(isHorizontal || isVertical, "马的数据不对");

            if (isHorizontal)
            {
                maBarrierPos = new Position(from.Row, (from.Column + to.Column) / 2);

                return !visitor.GetChessData(maBarrierPos).IsEmpty;
            }
            else
            {
                maBarrierPos = new Position((from.Row + to.Row) / 2, from.Column);

                return !visitor.GetChessData(maBarrierPos).IsEmpty;
            }
        }

        internal override bool IsAllowTo(Position toPos)
        {
            if (!base.IsAllowTo(toPos))
            {
                return false;
            }

            Position from = this.CurPos;
            var isHorizontal = Math.Abs(from.Row - toPos.Row) == 1 && Math.Abs(from.Column - toPos.Column) == 2;
            var isVertical = Math.Abs(from.Row - toPos.Row) == 2 && Math.Abs(from.Column - toPos.Column) == 1;

            return isHorizontal || isVertical;
        }
    }
}
