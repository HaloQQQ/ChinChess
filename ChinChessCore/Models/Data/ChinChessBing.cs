using ChinChessCore.Visitors;
using System;

namespace ChinChessCore.Models
{
    /// <summary>
    /// 过河前只能前进，过河后可以左右前
    /// </summary>
    public class ChinChessBing : InnerChinChess
    {
        public ChinChessBing(bool isRed) : base(isRed, ChessType.兵) { }

        public ChinChessBing(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.兵, isJieQi, isBack) { }

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

            Position from = this.CurPos;
            int fromRow = from.Row, fromColumn = from.Column;
            int toRow = toPos.Row, toColumn = toPos.Column;

            if (Math.Abs(fromRow - toRow) + Math.Abs(fromColumn - toColumn) != 1)
            {
                return false;
            }

            int rowStep = toRow - fromRow;

            if (rowStep != 0)
            {
                if (IsRed == true)
                {
                    if (rowStep > 0)
                    {
                        return false;
                    }
                }
                else
                {
                    if (rowStep < 0)
                    {
                        return false;
                    }
                }
            }

            return this.IsPosValid(toPos);
        }
    }
}
