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

        public override bool Visit(ChinChessJu chess, Position _)
        {
            if (!this.TryMoveCore(chess, _))
            {
                return false;
            }

            var up = MarkJu(chess, -1, 0);
            var down = MarkJu(chess, 1, 0);
            var left = MarkJu(chess, 0, -1);
            var right = MarkJu(chess, 0, 1);

            return up || down || left || right;

            bool MarkJu(ChinChessJu fromData, int rowStep, int columnStep)
            {
                int fromRow = fromData.CurPos.Row, fromColumn = fromData.CurPos.Column;

                bool hasChoice = false;

                int currentRow = fromRow + rowStep, currentColumn = fromColumn + columnStep;
                Position currentPos = new Position(currentRow, currentColumn);

                if (!fromData.IsPosValid(currentPos))
                {
                    return false;
                }

                if (columnStep != 0)
                {
                    if (!fromData.CanPutTo(_canPutToVisitor, currentPos))
                    {
                        return false;
                    }
                }

                ChinChessModel current = this.GetChess(currentPos);
                while (current.Data.IsEmpty)
                {
                    current.IsReadyToPut = true;

                    hasChoice = true;

                    currentPos = new Position(currentRow += rowStep, currentColumn += columnStep);
                    if (!fromData.IsPosValid(currentPos))
                    {
                        return true;
                    }

                    current = this.GetChess(currentPos);
                }

                if (fromData.IsEnemy(current.Data) == true)
                {
                    current.IsReadyToPut = true;

                    hasChoice = true;
                }

                return hasChoice;
            }
        }

        public override bool Visit(ChinChessPao chess, Position _)
        {
            if (!this.TryMoveCore(chess, _))
            {
                return false;
            }

            var up = MarkPao(chess, -1, 0);
            var down = MarkPao(chess, 1, 0);
            var left = MarkPao(chess, 0, -1);
            var right = MarkPao(chess, 0, 1);

            return up || down || left || right;

            bool MarkPao(ChinChessPao fromData, int rowStep, int columnStep)
            {
                bool hasChoice = false;

                int currentRow = fromData.CurPos.Row + rowStep, currentColumn = fromData.CurPos.Column + columnStep;
                var currentPos = new Position(currentRow, currentColumn);

                if (!fromData.IsPosValid(currentPos))
                {
                    return false;
                }

                if (columnStep != 0)
                {
                    if (this.GetChessData(currentPos).IsEmpty && !fromData.CanPutTo(_canPutToVisitor, currentPos))
                    {
                        return false;
                    }
                }

                int mountainsCount = 0;
                // 找空白 遇到 非空白 或者 边界 退出循环
                while (true)
                {
                    if (!fromData.IsPosValid(currentPos))
                    {
                        return hasChoice;
                    }

                    ChinChessModel current = this.GetChess(currentPos);
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
                            if (fromData.IsEnemy(current.Data) == true)
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

        public override bool Visit(ChinChessMa chess, Position _)
        {
            if (!this.TryMoveCore(chess, _))
            {
                return false;
            }

            // 左上 --
            var upLeft = MarkMa(chess, -2, -1);
            var leftUp = MarkMa(chess, -1, -2);

            // 左下 +-
            var downLeft = MarkMa(chess, +2, -1);
            var leftDown = MarkMa(chess, +1, -2);

            // 右上 -+
            var upRight = MarkMa(chess, -2, +1);
            var rightUp = MarkMa(chess, -1, +2);

            // 右下 ++
            var downRight = MarkMa(chess, +2, +1);
            var rightDown = MarkMa(chess, +1, +2);

            return upLeft || leftUp || downLeft || leftDown
                || upRight || rightUp || downRight || rightDown;

            bool MarkMa(ChinChessMa fromData, int rowStep, int columnStep)
            {
                int toRow = fromData.CurPos.Row + rowStep, toColumn = fromData.CurPos.Column + columnStep;
                Position toPos = new Position(toRow, toColumn);

                if (fromData.CanPutTo(_canPutToVisitor, toPos))
                {
                    this.GetChess(toPos).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        public override bool Visit(ChinChessXiang chess, Position _)
        {
            if (!this.TryMoveCore(chess, _))
            {
                return false;
            }

            var leftUp = MarkXiang(chess, -2, -2);
            var leftDown = MarkXiang(chess, 2, -2);
            var rightUp = MarkXiang(chess, -2, 2);
            var rightDown = MarkXiang(chess, 2, 2);

            return leftUp || leftDown || rightUp || rightDown;

            bool MarkXiang(ChinChessXiang fromData, int rowStep, int columnStep)
            {
                int toRow = fromData.CurPos.Row + rowStep, toColumn = fromData.CurPos.Column + columnStep;
                Position toPos = new Position(toRow, toColumn);

                if (fromData.CanPutTo(_canPutToVisitor, toPos))
                {
                    this.GetChess(toPos).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        public override bool Visit(ChinChessBing chess, Position _)
        {
            if (!this.TryMoveCore(chess, _))
            {
                return false;
            }

            var up = MarkBing(chess, -1, 0);
            var down = MarkBing(chess, 1, 0);
            var left = MarkBing(chess, 0, -1);
            var right = MarkBing(chess, 0, 1);

            return up || down || left || right;

            bool MarkBing(ChinChessBing fromData, int rowStep, int columnStep)
            {
                int toRow = fromData.CurPos.Row + rowStep, toColumn = fromData.CurPos.Column + columnStep;
                Position toPos = new Position(toRow, toColumn);

                if (fromData.CanPutTo(_canPutToVisitor, toPos))
                {
                    this.GetChess(toPos).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        public override bool Visit(ChinChessShi chess, Position _)
        {
            if (!this.TryMoveCore(chess, _))
            {
                return false;
            }

            var leftUp = MarkShi(chess, -1, -1);
            var leftDown = MarkShi(chess, 1, -1);
            var rightUp = MarkShi(chess, -1, 1);
            var rightDown = MarkShi(chess, 1, 1);

            return leftUp || leftDown || rightUp || rightDown;

            bool MarkShi(ChinChessShi fromData, int rowStep, int columnStep)
            {
                Position toPos = new Position(fromData.CurPos.Row + rowStep, fromData.CurPos.Column + columnStep);

                if (fromData.CanPutTo(_canPutToVisitor, toPos))
                {
                    this.GetChess(toPos).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        public override bool Visit(ChinChessShuai chess, Position _)
        {
            if (!this.TryMoveCore(chess, _))
            {
                return false;
            }

            var up = MarkShuai(chess, -1, 0);
            var down = MarkShuai(chess, 1, 0);
            var left = MarkShuai(chess, 0, -1);
            var right = MarkShuai(chess, 0, 1);

            return up || down || left || right;

            bool MarkShuai(ChinChessShuai fromData, int rowStep, int columnStep)
            {
                Position toPos = new Position(fromData.CurPos.Row + rowStep, fromData.CurPos.Column + columnStep);

                if (fromData.CanPutTo(_canPutToVisitor, toPos))
                {
                    this.GetChess(toPos).IsReadyToPut = true;

                    return true;
                }

                return false;
            }
        }

        protected override bool TryMoveCore(InnerChinChess chess, Position _)
        {
            if (chess.IsEmpty)
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