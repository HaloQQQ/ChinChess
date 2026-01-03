using ChinChessCore.Commands;
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

        public override bool Accept(IVisitor visitor, Position from, Position to)
            => visitor.Visit(this, from, to);

        public override bool CanLeave(ICanPutToVisitor canPutToVisitor, Position from, bool _ = true)
        {
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
                if (!this.CanPutTo(canPutToVisitor, from, item))
                {
                    continue; 
                }

                using (new MockMoveCommand(canPutToVisitor.GetChess(from), canPutToVisitor.GetChess(item))
                            .Execute())
                {
                    if (!this.IsDangerous(canPutToVisitor, item, out ChinChessModel _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetMaBarrier(IVisitor visitor, Position from, Position to, out Position maBarrierPos)
        {
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
    }
}
