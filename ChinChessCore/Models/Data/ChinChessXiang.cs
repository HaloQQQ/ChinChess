using ChinChessCore.Visitors;
using IceTea.Pure.Utils;
using System;

namespace ChinChessCore.Models
{
    public class ChinChessXiang : InnerChinChess
    {
        public ChinChessXiang(bool isRed) : base(isRed, ChessType.相) { }

        public ChinChessXiang(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.相, isJieQi, isBack) { }

        public override bool Accept(IVisitor visitor, Position to)
            => visitor.Visit(this, to);

        public override bool CanLeave(ICanPutToVisitor canPutToVisitor, bool? leaveInHorizontal = null)
        {
            Position from = this.CurPos;
            foreach (var item in new[] {
                                    new Position(from.Row - 2, from.Column - 2),
                                    new Position(from.Row - 2, from.Column + 2),
                                    new Position(from.Row + 2, from.Column - 2),
                                    new Position(from.Row + 2, from.Column + 2)
                                })
            {
                if (this.CanPutTo(canPutToVisitor, item))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetXiangBarrier(IVisitor visitor, Position to, out Position xiangBarrierPos)
        {
            Position from = this.CurPos;
            AppUtils.AssertDataValidation(
                Math.Abs(to.Row - from.Row) == 2 && Math.Abs(to.Column - from.Column) == 2,
                "象的数据不对");


            xiangBarrierPos = new Position((from.Row + to.Row) / 2, (from.Column + to.Column) / 2);

            return !visitor.GetChessData(xiangBarrierPos).IsEmpty;
        }

        internal override bool IsAllowTo(Position toPos)
        {
            if (!base.IsAllowTo(toPos))
            {
                return false;
            }

            Position from = this.CurPos;
            if (Math.Abs(toPos.Row - from.Row) != 2 || Math.Abs(toPos.Column - from.Column) != 2)
            {
                return false;
            }

            return this.IsPosValid(toPos);
        }
    }
}