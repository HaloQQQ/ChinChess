using ChinChessCore.Contracts;
using ChinChessCore.Visitors;
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

        public override bool Accept(IVisitor visitor, Position to)
            => visitor.Visit(this, to);

        /// <summary>
        /// 被盯上了，脱离危险
        /// 向垂直方向移动一步 or 通过吃子方式离开
        /// </summary>
        /// <param name="canPutToVisitor"></param>
        /// <param name="from">当前位置</param>
        /// <param name="isHorizontal">危险来向</param>
        /// <returns></returns>
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

            return this.TryGetTargetEnemy(canPutToVisitor, EnumDirection.Up, out _)
                || this.TryGetTargetEnemy(canPutToVisitor, EnumDirection.Down, out _)
                || this.TryGetTargetEnemy(canPutToVisitor, EnumDirection.Left, out _)
                || this.TryGetTargetEnemy(canPutToVisitor, EnumDirection.Right, out _);
        }

        /// <summary>
        /// 尝试获取可被击杀敌人的位置
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="fromPos"></param>
        /// <param name="enumDirection"></param>
        /// <param name="enemyPos"></param>
        /// <returns></returns>
        public bool TryGetTargetEnemy(IVisitor visitor, EnumDirection enumDirection, out Position enemyPos)
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

            Position fromPos = this.CurPos;
            int currentRow = fromPos.Row + rowStep, currentColumn = fromPos.Column + columnStep;
            Position currentPos = new Position(currentRow, currentColumn);
            int mountainsCount = 0;

            while (currentPos.IsValid)
            {
                InnerChinChess current = visitor.GetChessData(currentPos);
                if (!current.IsEmpty)
                {
                    mountainsCount++;

                    if (mountainsCount == 2)
                    {
                        if (this.IsEnemy(current) == true)
                        {
                            enemyPos = currentPos;
                            return true;
                        }

                        break;
                    }
                }

                currentPos = new Position(currentRow += rowStep, currentColumn += columnStep);
            }

            enemyPos = default;

            return false;
        }

        /// <summary>
        /// 获取炮架
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool TryGetPaoBarrier(ICanPutToVisitor visitor, Position to, out Position barrierPos)
        {
            Position from = this.CurPos;
            AppUtils.AssertDataValidation(visitor.GetChessData(from) == this, $"{this.Type}应该在{from.ToString()}");

            int rowStep = (to.Row == from.Row) ? 0 : (to.Row > from.Row ? 1 : -1);
            int columnStep = (to.Column == from.Column) ? 0 : (to.Column > from.Column ? 1 : -1);

            int currentRow = from.Row + rowStep, currentColumn = from.Column + columnStep;
            var pos = new Position(currentRow, currentColumn);
            int barrierCount = 0;
            Position tempPos = default;

            while (pos != to)
            {
                if (!visitor.GetChessData(pos).IsEmpty)
                {
                    barrierCount++;

                    if (barrierCount == 1)
                    {
                        tempPos = pos;
                    }
                }

                pos = new Position(currentRow += rowStep, currentColumn += columnStep);
            }

            barrierPos = tempPos;

            return barrierCount == 1;
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