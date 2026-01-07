using ChinChessCore.Commands;
using ChinChessCore.Visitors;
using System;

namespace ChinChessCore.Models
{
    public class ChinChessShuai : InnerChinChess
    {
        public ChinChessShuai(bool isRed) : base(isRed, ChessType.帥) { }

        public ChinChessShuai(bool isRed, bool isJieQi) : base(isRed, ChessType.帥, isJieQi, false) { }

        public override bool Accept(IVisitor visitor, Position to)
            => visitor.Visit(this, to);

        public override bool CanLeave(ICanPutToVisitor canPutToVisitor, bool? leaveInHorizontal = null)
        {
            Position pos = this.CurPos;
            foreach (var item in new Position[] {
                                new Position(pos.Row - 1, pos.Column),
                                new Position(pos.Row + 1, pos.Column),
                                new Position(pos.Row, pos.Column - 1),
                                new Position(pos.Row, pos.Column + 1)
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
                    var toChess = canPutToVisitor.GetChess(item);
                    using (new MockMoveCommand(
                                canPutToVisitor.GetChess(pos),
                                toChess
                            ).Execute()
                        )
                    {
                        if (!toChess.Data.IsDangerous(canPutToVisitor, out _))
                        {
                            return true;
                        }
                    }
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
            if (Math.Abs(toPos.Row - from.Row) + Math.Abs(toPos.Column - from.Column) != 1)
            {
                return false;
            }

            return this.IsPosValid(toPos);
        }
    }
}