using ChinChessCore.Commands;
using ChinChessCore.Models;
using System.Collections.Generic;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessCore.Visitors
{
    public interface ICanPutToVisitor : IVisitor { }

    /// <summary>
    /// 检测吃子或移动
    /// </summary>
    public class CanPutToVisitor : VisitorBase, ICanPutToVisitor
    {
        public CanPutToVisitor(IList<ChinChessModel> datas) : base(datas) { }

        public override bool Visit(ChinChessJu chess, Position to)
        {
            if (!this.TryMoveCore(chess, to))
            {
                return false;
            }

            int fromRow = chess.CurPos.Row, fromColumn = chess.CurPos.Column;
            int toRow = to.Row, toColumn = to.Column;

            var isSameRow = fromRow == toRow;
            var isSameColumn = fromColumn == toColumn;

            if (!isSameRow && !isSameColumn)
            {
                return false;
            }

            int rowStep = isSameRow ? 0 : (toRow > fromRow ? 1 : -1);
            int columnStep = isSameColumn ? 0 : (toColumn > fromColumn ? 1 : -1);

            int currentRow = fromRow + rowStep, currentColumn = fromColumn + columnStep;

            var currentPos = new Position(currentRow, currentColumn);
            while (currentPos != to)
            {
                if (!this.GetChessData(currentPos).IsEmpty)
                {
                    return false;
                }

                currentPos = new Position(
                    currentRow += rowStep,
                    currentColumn += columnStep);
            }

            return true;
        }

        public override bool Visit(ChinChessPao chess, Position to)
        {
            if (!this.TryMoveCore(chess, to))
            {
                return false;
            }

            Position from = chess.CurPos;
            int fromRow = from.Row, fromColumn = from.Column;
            int toRow = to.Row, toColumn = to.Column;

            // 移动
            var isSameRow = fromRow == toRow;
            var isSameColumn = fromColumn == toColumn;

            if (!isSameRow && !isSameColumn)
            {
                return false;
            }

            int rowStep = isSameRow ? 0 : (toRow > fromRow ? 1 : -1);
            int columnStep = isSameColumn ? 0 : (toColumn > fromColumn ? 1 : -1);

            int currentRow = fromRow + rowStep, currentColumn = fromColumn + columnStep;

            var mountainsCount = 0;
            var current = new Position(currentRow, currentColumn);
            while (current != to)
            {
                if (!this.GetChessData(current).IsEmpty)
                {
                    // 吃空子
                    if (this.GetChessData(to).IsEmpty)
                    {
                        return false;
                    }

                    // 超过一个支点
                    if (++mountainsCount == 2)
                    {
                        return false;
                    }
                }

                current = new Position(
                    currentRow += rowStep,
                    currentColumn += columnStep);
            }

            return true;
        }

        public override bool Visit(ChinChessBing chess, Position to)
        {
            if (!this.TryMoveCore(chess, to))
            {
                return false;
            }

            return chess.IsAllowTo(to);
        }

        public override bool Visit(ChinChessMa chess, Position to)
        {
            if (!this.TryMoveCore(chess, to))
            {
                return false;
            }

            if (!chess.IsAllowTo(to))
            {
                return false;
            }

            return !chess.TryGetMaBarrier(this, to, out _);
        }

        public override bool Visit(ChinChessXiang chess, Position to)
        {
            if (!this.TryMoveCore(chess, to))
            {
                return false;
            }

            if (!chess.IsAllowTo(to))
            {
                return false;
            }

            return !chess.TryGetXiangBarrier(this, to, out _);
        }

        public override bool Visit(ChinChessShi chess, Position to)
        {
            if (!this.TryMoveCore(chess, to))
            {
                return false;
            }

            return chess.IsAllowTo(to);
        }

        public override bool Visit(ChinChessShuai chess, Position to)
        {
            if (!this.TryMoveCore(chess, to))
            {
                return false;
            }

            if (!chess.IsAllowTo(to))
            {
                return false;
            }

            ChinChessModel targetChess = this.GetChess(to);
            using (new MockMoveCommand(this.GetChess(chess.CurPos), targetChess).Execute())
            {
                if (targetChess.Data.IsDangerous(this, out _))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 起点到终点必须不同
        /// 检测起点到终点的颜色及状态
        /// </summary>
        /// <param name="chess"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        protected override bool TryMoveCore(InnerChinChess chess, Position to)
        {
            if (!base.TryMoveCore(chess, to))
            {
                return false;
            }

            if (chess.IsEnemy(this.GetChessData(to)) == false)
            {
                return false;
            }

            if (chess.CurPos.Column != to.Column)
            {
                using (new MockMoveCommand(this.GetChess(chess.CurPos), this.GetChess(to)).Execute())
                {
                    if (this.FaceToFace())
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}