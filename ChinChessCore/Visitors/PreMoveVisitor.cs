using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using System.Collections.Generic;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessCore.Visitors
{
    public interface IPreMoveVisitor : IVisitor { }

    /// <summary>
    /// 选中棋子时显示可走的棋路
    /// 传入的To:Postion 不使用
    /// </summary>
    public class PreMoveVisitor : VisitorBase, IPreMoveVisitor
    {
        private ICanPutToVisitor _canPutToVisitor;

        public PreMoveVisitor(IList<ChinChessModel> datas, ICanPutToVisitor canPutToVisitor) : base(datas)
        {
            _canPutToVisitor = canPutToVisitor.AssertNotNull(nameof(canPutToVisitor));
        }

        public override bool Visit(ChinChessJu chess, Position from, Position _)
        {
            if (!this.TryMoveCore(chess, from, _))
            {
                return false;
            }

            var up = MarkJu(chess, from, -1, 0);
            var down = MarkJu(chess, from, 1, 0);
            var left = MarkJu(chess, from, 0, -1);
            var right = MarkJu(chess, from, 0, 1);

            return up || down || left || right;

            bool MarkJu(ChinChessJu fromData, Position _from, int rowStep, int columnStep)
            {
                int fromRow = _from.Row, fromColumn = _from.Column;

                bool hasChoice = false;

                int currentRow = fromRow + rowStep, currentColumn = fromColumn + columnStep;

                if (!fromData.IsPosValid(new Position(currentRow, currentColumn)))
                {
                    return false;
                }

                ChinChessModel current = this.GetChess(currentRow, currentColumn);
                while (current.Data.IsEmpty)
                {
                    current.IsReadyToPut = true;

                    hasChoice = true;

                    if (!fromData.IsPosValid(new Position(currentRow += rowStep, currentColumn += columnStep)))
                    {
                        return true;
                    }

                    current = this.GetChess(currentRow, currentColumn);
                }

                if (fromData.IsEnemy(current.Data))
                {
                    current.IsReadyToPut = true;

                    hasChoice = true;
                }

                return hasChoice;
            }
        }

        public override bool Visit(ChinChessPao chess, Position from, Position _)
        {
            if (!this.TryMoveCore(chess, from, _))
            {
                return false;
            }

            var up = MarkPao(chess, from, -1, 0);
            var down = MarkPao(chess, from, 1, 0);
            var left = MarkPao(chess, from, 0, -1);
            var right = MarkPao(chess, from, 0, 1);

            return up || down || left || right;

            bool MarkPao(ChinChessPao fromData, Position _from, int rowStep, int columnStep)
            {
                bool hasChoice = false;

                int currentRow = _from.Row + rowStep, currentColumn = _from.Column + columnStep;
                var currentPos = new Position(currentRow, currentColumn);

                int mountainsCount = 0;
                // 找空白 遇到 非空白 或者 边界 退出循环
                while (true)
                {
                    if (!fromData.IsPosValid(currentPos))
                    {
                        return hasChoice;
                    }

                    ChinChessModel current = this.GetChess(currentRow, currentColumn);
                    if (current.Data.IsEmpty)
                    {
                        if (mountainsCount == 0)
                        {
                            current.IsReadyToPut = true;
                            hasChoice = true;
                        }
                    }
                    else
                    {
                        if (++mountainsCount == 2)
                        {
                            if (fromData.IsEnemy(current.Data))
                            {
                                current.IsReadyToPut = true;
                                hasChoice = true;
                            }

                            return hasChoice;
                        }
                    }

                    currentPos = new Position(currentRow += rowStep, currentColumn += columnStep);
                }
            }
        }

        public override bool Visit(ChinChessMa chess, Position from, Position _)
        {
            if (!this.TryMoveCore(chess, from, _))
            {
                return false;
            }

            // 左上 --
            var upLeft = MarkMa(chess, from, -2, -1);
            var leftUp = MarkMa(chess, from, -1, -2);

            // 左下 +-
            var downLeft = MarkMa(chess, from, +2, -1);
            var leftDown = MarkMa(chess, from, +1, -2);

            // 右上 -+
            var upRight = MarkMa(chess, from, -2, +1);
            var rightUp = MarkMa(chess, from, -1, +2);

            // 右下 ++
            var downRight = MarkMa(chess, from, +2, +1);
            var rightDown = MarkMa(chess, from, +1, +2);

            return upLeft || leftUp || downLeft || leftDown
                || upRight || rightUp || downRight || rightDown;

            bool MarkMa(ChinChessMa fromData, Position _from, int rowStep, int columnStep)
            {
                int toRow = _from.Row + rowStep, toColumn = _from.Column + columnStep;

                if (fromData.Accept(_canPutToVisitor, from, new Position(toRow, toColumn)))
                {
                    this.GetChess(toRow, toColumn).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        public override bool Visit(ChinChessXiang chess, Position from, Position _)
        {
            if (!this.TryMoveCore(chess, from, _))
            {
                return false;
            }

            var leftUp = MarkXiang(chess, from, -2, -2);
            var leftDown = MarkXiang(chess, from, 2, -2);
            var rightUp = MarkXiang(chess, from, -2, 2);
            var rightDown = MarkXiang(chess, from, 2, 2);

            return leftUp || leftDown || rightUp || rightDown;

            bool MarkXiang(ChinChessXiang fromData, Position _from, int rowStep, int columnStep)
            {
                int toRow = _from.Row + rowStep, toColumn = _from.Column + columnStep;

                if (fromData.Accept(_canPutToVisitor, _from, new Position(toRow, toColumn)))
                {
                    this.GetChess(toRow, toColumn).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        public override bool Visit(ChinChessBing chess, Position from, Position _)
        {
            if (!this.TryMoveCore(chess, from, _))
            {
                return false;
            }

            var up = MarkBing(chess, from, -1, 0);
            var down = MarkBing(chess, from, 1, 0);
            var left = MarkBing(chess, from, 0, -1);
            var right = MarkBing(chess, from, 0, 1);

            return up || down || left || right;

            bool MarkBing(ChinChessBing fromData, Position _from, int rowStep, int columnStep)
            {
                int toRow = _from.Row + rowStep, toColumn = _from.Column + columnStep;

                if (fromData.Accept(_canPutToVisitor, from, new Position(toRow, toColumn)))
                {
                    this.GetChess(toRow, toColumn).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        public override bool Visit(ChinChessShi chess, Position from, Position _)
        {
            if (!this.TryMoveCore(chess, from, _))
            {
                return false;
            }

            var leftUp = MarkShi(chess, from, -1, -1);
            var leftDown = MarkShi(chess, from, 1, -1);
            var rightUp = MarkShi(chess, from, -1, 1);
            var rightDown = MarkShi(chess, from, 1, 1);

            return leftUp || leftDown || rightUp || rightDown;

            bool MarkShi(ChinChessShi fromData, Position _from, int rowStep, int columnStep)
            {
                int toRow = _from.Row + rowStep, toColumn = _from.Column + columnStep;

                if (fromData.Accept(_canPutToVisitor, from, new Position(toRow, toColumn)))
                {
                    this.GetChess(toRow, toColumn).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        public override bool Visit(ChinChessShuai chess, Position from, Position _)
        {
            if (!this.TryMoveCore(chess, from, _))
            {
                return false;
            }

            var up = MarkShuai(chess, from, -1, 0);
            var down = MarkShuai(chess, from, 1, 0);
            var left = MarkShuai(chess, from, 0, -1);
            var right = MarkShuai(chess, from, 0, 1);

            return up || down || left || right;

            bool MarkShuai(ChinChessShuai fromData, Position _from, int rowStep, int columnStep)
            {
                int toRow = _from.Row + rowStep, toColumn = _from.Column + columnStep;

                if (fromData.Accept(_canPutToVisitor, from, new Position(toRow, toColumn)))
                {
                    this.GetChess(toRow, toColumn).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        protected override bool TryMoveCore(InnerChinChess chess, Position from, Position _)
        {
            if (!base.TryMoveCore(chess, from, _))
            {
                return false;
            }

            if (!chess.IsPosValid(from))
            {
                return false;
            }

            this.GetChesses().ForEach(c => c.IsReadyToPut = false);

            return true;
        }

        protected override void DisposeCore()
        {
            _canPutToVisitor = null;

            base.DisposeCore();
        }
    }
}