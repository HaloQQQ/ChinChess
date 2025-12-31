using ChinChessCore.Contracts;
using ChinChessCore.Visitors;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using System;

namespace ChinChessCore.Models
{
    /// <summary>
    /// 隔山打牛
    /// </summary>
    public class ChinChessPao : InnerChinChess
    {
        public ChinChessPao(bool isRed) : base(isRed, ChessType.炮) { }

        public ChinChessPao(bool isRed, bool isJieQi, bool isBack) : base(isRed, ChessType.炮, isJieQi, isBack) { }

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
                if (this.IsPosValid_Abs(ChessType.炮, item, false)
                    && canPutToVisitor.GetChessData(item.Row, item.Column).IsEmpty
                    )
                {
                    return true;
                }
            }

            return this.TryGetTargetEnemy(canPutToVisitor, from, isHorizontal ? EnumDirection.Up : EnumDirection.Left, out _) 
                || this.TryGetTargetEnemy(canPutToVisitor, from, isHorizontal ? EnumDirection.Down : EnumDirection.Right, out _);
        }

        public bool TryGetTargetEnemy(IVisitor visitor, Position fromPos, EnumDirection enumDirection, out Position enemyPos)
        {
            int rowStep = 0, columnStep = 0;
            switch (enumDirection)
            {
                case EnumDirection.Up:
                    rowStep = -1;
                    break;
                case EnumDirection.Down:
                    rowStep = 1;
                    break;
                case EnumDirection.Left:
                    columnStep = -1;
                    break;
                case EnumDirection.Right:
                    columnStep = 1;
                    break;
                default:
                    break;
            }

            int currentRow = fromPos.Row + rowStep, currentColumn = fromPos.Column + columnStep;

            int mountainsCount = 0;

            while (currentRow.IsInRange(0, 9) && currentColumn.IsInRange(0, 8))
            {
                InnerChinChess current = visitor.GetChessData(currentRow, currentColumn);
                if (!current.IsEmpty)
                {
                    mountainsCount++;

                    if (mountainsCount == 2)
                    {
                        if (this.IsEnemy(current))
                        {
                            enemyPos = new Position(currentRow, currentColumn);
                            return true;
                        }

                        break;
                    }
                }

                currentRow += rowStep;
                currentColumn += columnStep;
            }

            enemyPos = default;

            return false;
        }

        public Position GetPaoBarrier(ICanPutToVisitor visitor, Position from, Position to)
        {
            AppUtils.AssertDataValidation(visitor.GetChessData(from.Row, from.Column) == this, $"{this.Type}应该在{from.ToString()}");

            int rowStep = (to.Row == from.Row) ? 0 : (to.Row > from.Row ? 1 : -1);
            int columnStep = (to.Column == from.Column) ? 0 : (to.Column > from.Column ? 1 : -1);

            int currentRow = from.Row + rowStep, currentColumn = from.Column + columnStep;
            var pos = new Position(currentRow, currentColumn);

            while (pos != to)
            {
                if (!visitor.GetChessData(currentRow, currentColumn).IsEmpty)
                {
                    return pos;
                }

                pos = new Position(currentRow += rowStep, currentColumn += columnStep);
            }

            throw new InvalidOperationException();
        }
    }
}