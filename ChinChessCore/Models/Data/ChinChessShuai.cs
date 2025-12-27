using ChinChessCore.Commands;
using ChinChessCore.Visitors;

namespace ChinChessCore.Models
{
    public class ChinChessShuai : InnerChinChess
    {
        public ChinChessShuai(bool isRed) : base(isRed, ChessType.帥) { }

        public ChinChessShuai(bool isRed, bool isJieQi) : base(isRed, ChessType.帥, isJieQi, false) { }

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

        public bool SelfRescue(ICanPutToVisitor canPutToVisitor, Position selfPos)
        {
            foreach (var item in new Position[] {
                                new Position(selfPos.Row - 1, selfPos.Column),
                                new Position(selfPos.Row + 1, selfPos.Column),
                                new Position(selfPos.Row, selfPos.Column - 1),
                                new Position(selfPos.Row, selfPos.Column + 1)
                            })
            {
                if (this.CanPutTo(canPutToVisitor, selfPos, item))
                {
                    using (new MockMoveCommand(
                                canPutToVisitor.GetChess(selfPos.Row, selfPos.Column),
                                canPutToVisitor.GetChess(item.Row, item.Column)
                            ).Execute()
                        )
                    {
                        if (!canPutToVisitor.GetChessData(item.Row, item.Column)
                                .IsDangerous(canPutToVisitor, item, out _))
                        {
                            if (!canPutToVisitor.FaceToFace())
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}