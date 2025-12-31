using ChinChessCore.Commands;
using ChinChessCore.Models;
using IceTea.Pure.Utils;
using System.Collections.Generic;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessCore.Visitors
{
    public interface IGuardVisitor : IVisitor
    {
    }

    /// <summary>
    /// 棋子 已遇到危险，防止被 杀手 击杀
    /// 1、威胁可被击杀
    /// 2、替死
    /// </summary>
    public class GuardVisitor : VisitorBase, IGuardVisitor
    {
        private ICanPutToVisitor _canPutToVisitor;

        public GuardVisitor(IList<ChinChessModel> datas, ICanPutToVisitor canPutToVisitor) : base(datas)
        {
            _canPutToVisitor = canPutToVisitor.AssertNotNull(nameof(canPutToVisitor));
        }

        public override bool Visit(ChinChessJu killer, Position killerPos, Position victimPos)
        {
            if (!this.TryMoveCore(killer, killerPos, victimPos))
            {
                return true;
            }

            if (this.ProtectByEatKiller(killer, killerPos, victimPos))
            {
                return true;
            }

            return this.FenceFromJuOrPao(killer, killerPos, victimPos);
        }

        public override bool Visit(ChinChessPao killer, Position killerPos, Position victimPos)
        {
            if (!this.TryMoveCore(killer, killerPos, victimPos))
            {
                return true;
            }

            if (this.ProtectByEatKiller(killer, killerPos, victimPos))
            {
                return true;
            }

            var barrier = killer.GetPaoBarrier(_canPutToVisitor, killerPos, victimPos);

            var barrierData = this.GetChessData(barrier.Row, barrier.Column);
            // 支架为敌军
            if (killer.IsEnemy(barrierData))
            {
                // 士、相、马、兵、車、炮
                if (barrierData.CanLeave(_canPutToVisitor, barrier, killerPos.Row == victimPos.Row))
                {
                    return true;
                }
            }

            return this.FenceFromJuOrPao(killer, barrier, victimPos);
        }

        /// <summary>
        /// 从車、炮手中救人
        /// </summary>
        /// <param name="chess"></param>
        /// <param name="fromPos"></param>
        /// <param name="toPos"></param>
        /// <returns></returns>
        private bool FenceFromJuOrPao(InnerChinChess chess, Position fromPos, Position toPos)
        {
            int fromRow = fromPos.Row, fromColumn = fromPos.Column;
            int toRow = toPos.Row, toColumn = toPos.Column;

            var isSameRow = fromRow == toRow;
            var isSameColumn = fromColumn == toColumn;

            int rowStep = (toRow == fromRow) ? 0 : (toRow < fromRow ? 1 : -1);
            int columnStep = (toColumn == fromColumn) ? 0 : (toColumn < fromColumn ? 1 : -1);

            int currentRow = toRow + rowStep, currentColumn = toColumn + columnStep;
            var pos = new Position(currentRow, currentColumn);

            #region 士
            while (fromPos != pos && chess.IsPosValid_Rel(ChessType.仕, pos))
            {
                if (this.ProtectByShield(chess, pos, toPos, ChessType.仕, new[]
                        {
                            new Position(pos.Row - 1, pos.Column - 1),
                            new Position(pos.Row - 1, pos.Column + 1),
                            new Position(pos.Row + 1, pos.Column - 1),
                            new Position(pos.Row + 1, pos.Column + 1)
                        })
                    )
                {
                    return true;
                }

                pos = new Position(currentRow += rowStep, currentColumn += columnStep);
            }
            #endregion

            #region 相
            currentRow = toRow + rowStep; currentColumn = toColumn + columnStep;
            pos = new Position(currentRow, currentColumn);

            while (fromPos != pos && chess.IsPosValid_Rel(ChessType.相, pos))
            {
                if (this.ProtectByShield(chess, pos, toPos, ChessType.相, new[]
                {
                new Position(pos.Row - 2, pos.Column - 2),
                new Position(pos.Row - 2, pos.Column + 2),
                new Position(pos.Row + 2, pos.Column - 2),
                new Position(pos.Row + 2, pos.Column + 2)
            }))
                {
                    return true;
                }

                pos = new Position(currentRow += rowStep, currentColumn += columnStep);
            }
            #endregion

            #region 兵
            currentRow = toRow + rowStep; currentColumn = toColumn + columnStep;
            pos = new Position(currentRow, currentColumn);

            while (pos != fromPos)
            {
                if (this.ProtectByShield(chess, pos, toPos, ChessType.兵, new[]
                {
                new Position(pos.Row - 1, pos.Column),
                new Position(pos.Row + 1, pos.Column),
                new Position(pos.Row, pos.Column - 1),
                new Position(pos.Row, pos.Column + 1)
            }))
                {
                    return true;
                }

                pos = new Position(currentRow += rowStep, currentColumn += columnStep);
            }
            #endregion

            #region 马
            currentRow = toRow + rowStep; currentColumn = toColumn + columnStep;
            pos = new Position(currentRow, currentColumn);

            while (pos != fromPos)
            {
                if (this.ProtectByShield(chess, pos, toPos, ChessType.馬, new[]
                {
                new Position(toRow - 2, toColumn - 1),
                new Position(toRow - 1, toColumn - 2),
                new Position(toRow - 2, toColumn + 1),
                new Position(toRow - 1, toColumn + 2),
                new Position(toRow + 2, toColumn - 1),
                new Position(toRow + 1, toColumn - 2),
                new Position(toRow + 2, toColumn + 1),
                new Position(toRow + 1, toColumn + 2)
            }))
                {
                    return true;
                }

                pos = new Position(currentRow += rowStep, currentColumn += columnStep);
            }
            #endregion

            #region 車、炮  垫
            currentRow = toRow + rowStep; currentColumn = toColumn + columnStep;
            pos = new Position(currentRow, currentColumn);

            while (pos != fromPos)
            {
                if (isSameColumn)
                {
                    if (MoveColumn(pos.Column, -1) || MoveColumn(pos.Column, 1))
                    {
                        return true;
                    }

                    bool MoveColumn(int column, int cStep)
                    {
                        var currentPos = new Position(currentRow, column += cStep);

                        while (true)
                        {
                            if (!chess.IsPosValid_Abs(ChessType.炮 | ChessType.車, currentPos))
                            {
                                return false;
                            }

                            if (!this.GetChessData(currentRow, column).IsEmpty)
                            {
                                break;
                            }

                            currentPos = new Position(currentRow, column += cStep);
                        }

                        if (chess.IsEnemy(this, currentPos, ChessType.車 | ChessType.炮))
                        {
                            using (new MockMoveCommand(
                                    this.GetChess(currentRow, column),
                                    this.GetChess(pos.Row, pos.Column)
                                ).Execute()
                            )
                            {
                                if (this.FaceToFace())
                                {
                                    return false;
                                }

                                if (!this.GetChessData(toPos.Row, toPos.Column).IsDangerous(_canPutToVisitor, toPos, out _))
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }
                }
                else
                {
                    if (MoveRow(pos.Row, -1) || MoveRow(pos.Row, 1))
                    {
                        return true;
                    }

                    bool MoveRow(int row, int rStep)
                    {
                        var currentPos = new Position(row += rStep, currentColumn);

                        while (true)
                        {
                            if (!chess.IsPosValid_Abs(ChessType.炮 | ChessType.車, currentPos))
                            {
                                return false;
                            }

                            if (!this.GetChessData(row, currentColumn).IsEmpty)
                            {
                                break;
                            }

                            currentPos = new Position(row += rStep, currentColumn);
                        }

                        if (chess.IsEnemy(this, currentPos, ChessType.車 | ChessType.炮))
                        {
                            using (new MockMoveCommand(
                                    this.GetChess(row, currentColumn),
                                    this.GetChess(pos.Row, pos.Column)
                                ).Execute()
                            )
                            {
                                if (this.FaceToFace())
                                {
                                    return false;
                                }

                                if (!this.GetChessData(toPos.Row, toPos.Column).IsDangerous(_canPutToVisitor, toPos, out _))
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }
                }

                pos = new Position(currentRow += rowStep, currentColumn += columnStep);
            }
            #endregion

            #region 炮  抽
            if (chess.Type == ChessType.炮)
            {
                if(_canPutToVisitor.GetChessData(fromPos.Row, fromPos.Column)
                    .CanLeave(_canPutToVisitor, fromPos, fromPos.Row == toPos.Row))
                {
                    return true;
                }
            }
            #endregion

            return false;
        }

        public override bool Visit(ChinChessMa killer, Position killerPos, Position victimPos)
        {
            if (!this.TryMoveCore(killer, killerPos, victimPos))
            {
                return true;
            }

            if (this.ProtectByEatKiller(killer, killerPos, victimPos))
            {
                return true;
            }

            var barrierPos = killer.GetMaBarrier(killerPos, victimPos);

            #region 士
            if (this.ProtectByShield(killer, barrierPos, victimPos, ChessType.仕, new[] {
                new Position(barrierPos.Row - 1, barrierPos.Column - 1),
                new Position(barrierPos.Row - 1, barrierPos.Column + 1),
                new Position(barrierPos.Row + 1, barrierPos.Column - 1),
                new Position(barrierPos.Row + 1, barrierPos.Column + 1)
            }))
            {
                return true;
            }
            #endregion

            #region 相
            if (this.ProtectByShield(killer, barrierPos, victimPos, ChessType.相, new[] {
                new Position(barrierPos.Row - 2, barrierPos.Column - 2),
                new Position(barrierPos.Row - 2, barrierPos.Column + 2),
                new Position(barrierPos.Row + 2, barrierPos.Column - 2),
                new Position(barrierPos.Row + 2, barrierPos.Column + 2)
            }))
            {
                return true;
            }
            #endregion

            #region 兵
            if (this.ProtectByShield(killer, barrierPos, victimPos, ChessType.兵, new[] {
                new Position(barrierPos.Row - 1, barrierPos.Column),
                new Position(barrierPos.Row + 1, barrierPos.Column),
                new Position(barrierPos.Row, barrierPos.Column - 1),
                new Position(barrierPos.Row, barrierPos.Column + 1)
            }))
            {
                return true;
            }
            #endregion

            #region 马
            if (this.ProtectByShield(killer, barrierPos, victimPos, ChessType.馬, new[] {
                new Position(barrierPos.Row - 2, barrierPos.Row - 1),
                new Position(barrierPos.Row - 1, barrierPos.Row - 2),
                new Position(barrierPos.Row - 2, barrierPos.Row + 1),
                new Position(barrierPos.Row - 1, barrierPos.Row + 2),
                new Position(barrierPos.Row + 2, barrierPos.Row - 1),
                new Position(barrierPos.Row + 1, barrierPos.Row - 2),
                new Position(barrierPos.Row + 2, barrierPos.Row + 1),
                new Position(barrierPos.Row + 1, barrierPos.Row + 2)
            }))
            {
                return true;
            }
            #endregion

            #region 車、炮
            if (ComeFrom(barrierPos, -1, 0) || ComeFrom(barrierPos, 1, 0)
                || ComeFrom(barrierPos, 0, -1) || ComeFrom(barrierPos, 0, 1)
                )
            {
                return true;
            }

            bool ComeFrom(Position to, int rStep, int cStep)
            {
                int currentRow = to.Row, currentColumn = to.Column;
                var currentPos = new Position(currentRow += rStep, currentColumn += cStep);

                while (true)
                {
                    if (!killer.IsPosValid_Abs(ChessType.炮 | ChessType.車, currentPos))
                    {
                        return false;
                    }

                    if (!this.GetChessData(currentRow, currentColumn).IsEmpty)
                    {
                        break;
                    }

                    currentPos = new Position(currentRow += rStep, currentColumn += cStep);
                }

                if (killer.IsEnemy(this, currentPos, ChessType.車 | ChessType.炮))
                {
                    using (new MockMoveCommand(
                            this.GetChess(currentRow, currentColumn),
                            this.GetChess(to.Row, to.Column)
                        ).Execute()
                    )
                    {
                        if (this.FaceToFace())
                        {
                            return false;
                        }

                        if (!this.GetChessData(victimPos.Row, victimPos.Column).IsDangerous(_canPutToVisitor, victimPos, out _))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            #endregion

            return false;
        }

        public override bool Visit(ChinChessXiang killer, Position killerPos, Position victimPos)
        {
            if (!this.TryMoveCore(killer, killerPos, victimPos))
            {
                return true;
            }

            if (this.ProtectByEatKiller(killer, killerPos, victimPos))
            {
                return true;
            }

            var barrierPos = killer.GetXiangBarrier(killerPos, victimPos);

            #region 士
            if (this.ProtectByShield(killer, barrierPos, victimPos, ChessType.仕, new[] {
                new Position(barrierPos.Row - 1, barrierPos.Column - 1),
                new Position(barrierPos.Row - 1, barrierPos.Column + 1),
                new Position(barrierPos.Row + 1, barrierPos.Column - 1),
                new Position(barrierPos.Row + 1, barrierPos.Column + 1)
            }))
            {
                return true;
            }
            #endregion

            #region 相
            if (this.ProtectByShield(killer, barrierPos, victimPos, ChessType.相, new[] {
                new Position(barrierPos.Row - 2, barrierPos.Column - 2),
                new Position(barrierPos.Row - 2, barrierPos.Column + 2),
                new Position(barrierPos.Row + 2, barrierPos.Column - 2),
                new Position(barrierPos.Row + 2, barrierPos.Column + 2)
            }))
            {
                return true;
            }
            #endregion

            #region 兵
            if (this.ProtectByShield(killer, barrierPos, victimPos, ChessType.兵, new[] {
                new Position(barrierPos.Row - 1, barrierPos.Column),
                new Position(barrierPos.Row + 1, barrierPos.Column),
                new Position(barrierPos.Row, barrierPos.Column - 1),
                new Position(barrierPos.Row, barrierPos.Column + 1)
            }))
            {
                return true;
            }
            #endregion

            #region 马
            if (this.ProtectByShield(killer, barrierPos, victimPos, ChessType.馬, new[] {
                new Position(barrierPos.Row - 2, barrierPos.Row - 1),
                new Position(barrierPos.Row - 1, barrierPos.Row - 2),
                new Position(barrierPos.Row - 2, barrierPos.Row + 1),
                new Position(barrierPos.Row - 1, barrierPos.Row + 2),
                new Position(barrierPos.Row + 2, barrierPos.Row - 1),
                new Position(barrierPos.Row + 1, barrierPos.Row - 2),
                new Position(barrierPos.Row + 2, barrierPos.Row + 1),
                new Position(barrierPos.Row + 1, barrierPos.Row + 2)
            }))
            {
                return true;
            }
            #endregion

            #region 車、炮
            if (ComeFrom(barrierPos, -1, 0) || ComeFrom(barrierPos, 1, 0)
                || ComeFrom(barrierPos, 0, -1) || ComeFrom(barrierPos, 0, 1)
                )
            {
                return true;
            }

            bool ComeFrom(Position to, int rStep, int cStep)
            {
                int currentRow = to.Row, currentColumn = to.Column;
                var currentPos = new Position(currentRow += rStep, currentColumn += cStep);

                while (true)
                {
                    if (!killer.IsPosValid_Abs(ChessType.炮 | ChessType.車, currentPos))
                    {
                        return false;
                    }

                    if (!this.GetChessData(currentRow, currentColumn).IsEmpty)
                    {
                        break;
                    }

                    currentPos = new Position(currentRow += rStep, currentColumn += cStep);
                }

                if (killer.IsEnemy(this, currentPos, ChessType.車 | ChessType.炮))
                {
                    using (new MockMoveCommand(
                            this.GetChess(currentRow, currentColumn),
                            this.GetChess(to.Row, to.Column)
                        ).Execute()
                    )
                    {
                        if (this.FaceToFace())
                        {
                            return false;
                        }

                        if (!this.GetChessData(victimPos.Row, victimPos.Column).IsDangerous(_canPutToVisitor, victimPos, out _))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            #endregion

            return false;
        }

        public override bool Visit(ChinChessBing killer, Position killerPos, Position victimPos)
        {
            if (!this.TryMoveCore(killer, killerPos, victimPos))
            {
                return true;
            }

            return this.ProtectByEatKiller(killer, killerPos, victimPos);
        }

        public override bool Visit(ChinChessShi killer, Position killerPos, Position victimPos)
        {
            if (!this.TryMoveCore(killer, killerPos, victimPos))
            {
                return true;
            }

            return this.ProtectByEatKiller(killer, killerPos, victimPos);
        }

        public override bool Visit(ChinChessShuai killer, Position killerPos, Position victimPos)
        {
            if (!this.TryMoveCore(killer, killerPos, victimPos))
            {
                return true;
            }

            return this.ProtectByEatKiller(killer, killerPos, victimPos);
        }

        private bool ProtectByShield(InnerChinChess killer, Position guardToPos, Position victimPos,
                                ChessType chessType,
                                IList<Position> guardFromPos)
        {
            foreach (var item in guardFromPos)
            {
                if (killer.TryPutToIfIsEnemy(_canPutToVisitor, item, guardToPos, chessType))
                {
                    using (new MockMoveCommand(
                                this.GetChess(item.Row, item.Column),
                                this.GetChess(guardToPos.Row, guardToPos.Column)
                            ).Execute()
                        )
                    {
                        if (this.FaceToFace())
                        {
                            return false;
                        }

                        if (!this.GetChessData(victimPos).IsDangerous(_canPutToVisitor, victimPos, out _))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool ProtectByEatKiller(InnerChinChess killer, Position killerPos, Position victimPos)
        {
            if (killer.IsDangerous(_canPutToVisitor, killerPos, out ChinChessModel guard))
            {
                var guardIsShuai = guard.Data.Type == ChessType.帥;

                // 帅后续可以自救，此处不需要尝试自保
                if (guardIsShuai && victimPos == guard.Pos)
                {
                    return false;
                }

                using (new MockMoveCommand(
                                this.GetChess(guard.Pos.Row, guard.Pos.Column),
                                this.GetChess(killerPos.Row, killerPos.Column)
                            ).Execute()
                        )
                {
                    if (this.FaceToFace())
                    {
                        return false;
                    }

                    if (guardIsShuai)
                    {
                        if (this.GetChessData(killerPos).IsDangerous(_canPutToVisitor, killerPos, out _))
                        {
                            return false;
                        }
                    }

                    if (this.GetChessData(victimPos).IsDangerous(_canPutToVisitor, victimPos, out _))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查坐标
        /// 检查双方阵营
        /// 检查杀手是否可被击杀
        /// </summary>
        /// <param name="killer"></param>
        /// <param name="killerPos"></param>
        /// <param name="victimPos"></param>
        /// <returns></returns>
        protected override bool TryMoveCore(InnerChinChess killer, Position killerPos, Position victimPos)
        {
            if (!base.TryMoveCore(killer, killerPos, victimPos))
            {
                return false;
            }

            if (killerPos == victimPos)
            {
                return false;
            }

            if (!killer.IsPosValid(killerPos))
            {
                return false;
            }

            var targetData = this.GetChessData(victimPos.Row, victimPos.Column);

            bool prevent = killer.IsEmpty || !killer.IsEnemy(targetData);

            if (prevent)
            {
                return false;
            }

            return true;
        }

        protected override void DisposeCore()
        {
            _canPutToVisitor = null;

            base.DisposeCore();
        }
    }
}