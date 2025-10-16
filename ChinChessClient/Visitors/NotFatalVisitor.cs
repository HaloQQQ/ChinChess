using ChinChessClient.Commands;
using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Pure.Utils;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.Visitors;

/// <summary>
/// 帅 已遇到危险，防止被 杀手 击杀
/// 1、帅 自救
/// 2、威胁可被击杀
/// 3、救驾
/// </summary>
internal class NotFatalVisitor : VisitorBase
{
    private IVisitor _canEatVisitor;

    public NotFatalVisitor(IList<ChinChessModel> datas, IVisitor canEatVisitor) : base(datas)
    {
        _canEatVisitor = canEatVisitor.AssertNotNull(nameof(canEatVisitor));
    }

    public override bool Visit(ChinChessJu chess, Position killerPos, Position shuaiPos)
    {
        if (!this.TryMoveCore(chess, killerPos, shuaiPos))
        {
            return true;
        }

        return this.FenceFromJuOrPao(chess, killerPos, shuaiPos);
    }

    public override bool Visit(ChinChessPao chess, Position killerPos, Position shuaiPos)
    {
        if (!this.TryMoveCore(chess, killerPos, shuaiPos))
        {
            return true;
        }

        var barrier = chess.GetPaoBarrier(_canEatVisitor, killerPos, shuaiPos);

        var barrierData = this.GetChessData(barrier.Row, barrier.Column);
        // 支架为敌军
        if (chess.IsEnemy(barrierData))
        {
            // 士、相、马、兵、車、炮
            if (barrierData.CanLeave(_canEatVisitor, barrier, killerPos.Column == shuaiPos.Column))
            {
                return true;
            }
        }

        return this.FenceFromJuOrPao(chess, barrier, shuaiPos);
    }

    /// <summary>
    /// 牺牲自己拯救帅
    /// </summary>
    /// <param name="chess"></param>
    /// <param name="from"></param>
    /// <param name="shuaiPos"></param>
    /// <returns></returns>
    private bool FenceFromJuOrPao(InnerChinChess chess, Position from, Position shuaiPos)
    {
        int fromRow = from.Row, fromColumn = from.Column;
        int toRow = shuaiPos.Row, toColumn = shuaiPos.Column;

        var isSameRow = fromRow == toRow;
        var isSameColumn = fromColumn == toColumn;

        int rowStep = (toRow == fromRow) ? 0 : (toRow < fromRow ? 1 : -1);
        int columnStep = (toColumn == fromColumn) ? 0 : (toColumn < fromColumn ? 1 : -1);

        int currentRow = toRow + rowStep, currentColumn = toColumn + columnStep;
        var pos = new Position(currentRow, currentColumn);

        #region 士
        while (from != pos && chess.IsPoseValid_Rel(ChessType.仕, pos))
        {
            if (this.TryProtect(chess, pos, shuaiPos, ChessType.仕, new[]
            {
                new Position(pos.Row - 1, pos.Column - 1),
                new Position(pos.Row - 1, pos.Column + 1),
                new Position(pos.Row + 1, pos.Column - 1),
                new Position(pos.Row + 1, pos.Column + 1)
            }))
            {
                return true;
            }

            pos = new Position(currentRow += rowStep, currentColumn += columnStep);
        }
        #endregion

        #region 相
        currentRow = toRow + rowStep; currentColumn = toColumn + columnStep;
        pos = new Position(currentRow, currentColumn);

        while (from != pos && chess.IsPoseValid_Rel(ChessType.相, pos))
        {
            if (this.TryProtect(chess, pos, shuaiPos, ChessType.相, new[]
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

        while (pos != from)
        {
            if (this.TryProtect(chess, pos, shuaiPos, ChessType.兵, new[]
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

        while (pos != from)
        {
            if (this.TryProtect(chess, pos, shuaiPos, ChessType.馬, new[]
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

        #region 車、炮
        currentRow = toRow + rowStep; currentColumn = toColumn + columnStep;
        pos = new Position(currentRow, currentColumn);

        while (pos != from)
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
                            if (chess.FaceToFace(_canEatVisitor))
                            {
                                return false;
                            }

                            if (!this.GetChessData(shuaiPos.Row, shuaiPos.Column).IsDangerous(_canEatVisitor, shuaiPos, out _))
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
                            if (chess.FaceToFace(_canEatVisitor))
                            {
                                return false;
                            }

                            if (!this.GetChessData(shuaiPos.Row, shuaiPos.Column).IsDangerous(_canEatVisitor, shuaiPos, out _))
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

        return false;
    }

    public override bool Visit(ChinChessMa chess, Position killerPos, Position shuaiPos)
    {
        if (!this.TryMoveCore(chess, killerPos, shuaiPos))
        {
            return true;
        }

        var barrierPos = chess.GetMaBarrier(killerPos, shuaiPos);

        #region 士
        if (this.TryProtect(chess, barrierPos, shuaiPos, ChessType.仕, new[] {
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
        if (this.TryProtect(chess, barrierPos, shuaiPos, ChessType.相, new[] {
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
        if (this.TryProtect(chess, barrierPos, shuaiPos, ChessType.兵, new[] {
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
        if (this.TryProtect(chess, barrierPos, shuaiPos, ChessType.馬, new[] {
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
                if (!chess.IsPosValid_Abs(ChessType.炮 | ChessType.車, currentPos))
                {
                    return false;
                }

                if (!this.GetChessData(currentRow, currentColumn).IsEmpty)
                {
                    break;
                }

                currentPos = new Position(currentRow += rStep, currentColumn += cStep);
            }

            if (chess.IsEnemy(this, currentPos, ChessType.車 | ChessType.炮))
            {
                using (new MockMoveCommand(
                        this.GetChess(currentRow, currentColumn),
                        this.GetChess(to.Row, to.Column)
                    ).Execute()
                )
                {
                    if (chess.FaceToFace(_canEatVisitor))
                    {
                        return false;
                    }

                    if (!this.GetChessData(shuaiPos.Row, shuaiPos.Column).IsDangerous(_canEatVisitor, shuaiPos, out _))
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

    public override bool Visit(ChinChessBing chess, Position killerPos, Position shuaiPos)
        => !this.TryMoveCore(chess, killerPos, shuaiPos);

    public override bool Visit(ChinChessXiang chess, Position killerPos, Position shuaiPos)
    {
        if (!this.TryMoveCore(chess, killerPos, shuaiPos))
        {
            return true;
        }

        var barrierPos = chess.GetXiangBarrier(killerPos, shuaiPos);

        #region 士
        if (this.TryProtect(chess, barrierPos, shuaiPos, ChessType.仕, new[] {
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
        if (this.TryProtect(chess, barrierPos, shuaiPos, ChessType.相, new[] {
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
        if (this.TryProtect(chess, barrierPos, shuaiPos, ChessType.兵, new[] {
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
        if (this.TryProtect(chess, barrierPos, shuaiPos, ChessType.馬, new[] {
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
                if (!chess.IsPosValid_Abs(ChessType.炮 | ChessType.車, currentPos))
                {
                    return false;
                }

                if (!this.GetChessData(currentRow, currentColumn).IsEmpty)
                {
                    break;
                }

                currentPos = new Position(currentRow += rStep, currentColumn += cStep);
            }

            if (chess.IsEnemy(this, currentPos, ChessType.車 | ChessType.炮))
            {
                using (new MockMoveCommand(
                        this.GetChess(currentRow, currentColumn),
                        this.GetChess(to.Row, to.Column)
                    ).Execute()
                )
                {
                    if (chess.FaceToFace(_canEatVisitor))
                    {
                        return false;
                    }

                    if (!this.GetChessData(shuaiPos.Row, shuaiPos.Column).IsDangerous(_canEatVisitor, shuaiPos, out _))
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

    public override bool Visit(ChinChessShi chess, Position killerPos, Position shuaiPos)
        => !this.TryMoveCore(chess, killerPos, shuaiPos);

    public override bool Visit(ChinChessShuai chess, Position from, Position to)
        => throw new NotImplementedException();

    private bool TryProtect(InnerChinChess chess, Position pos, Position shuai,
                            ChessType chessType,
                            IList<Position> positions)
    {
        foreach (var item in positions)
        {
            if (chess.CanEnemyTo(_canEatVisitor, item, pos, chessType))
            {
                using (new MockMoveCommand(
                            this.GetChess(item.Row, item.Column),
                            this.GetChess(pos.Row, pos.Column)
                        ).Execute()
                    )
                {
                    if (chess.FaceToFace(_canEatVisitor))
                    {
                        return false;
                    }

                    if (!this.GetChessData(shuai.Row, shuai.Column).IsDangerous(_canEatVisitor, shuai, out _))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool SelfRescue(InnerChinChess chess, Position shuai)
    {
        foreach (var item in new Position[] {
                                new Position(shuai.Row - 1, shuai.Column),
                                new Position(shuai.Row + 1, shuai.Column),
                                new Position(shuai.Row, shuai.Column - 1),
                                new Position(shuai.Row, shuai.Column + 1)
                            })
        {
            if (chess.CanEnemyTo(_canEatVisitor, shuai, item, ChessType.帥))
            {
                using (new MockMoveCommand(
                            this.GetChess(shuai.Row, shuai.Column),
                            this.GetChess(item.Row, item.Column)
                        ).Execute()
                    )
                {
                    if (!this.GetChessData(item.Row, item.Column).IsDangerous(_canEatVisitor, item, out _))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    protected override bool TryMoveCore(InnerChinChess chess, Position from, Position to)
    {
        if (!base.TryMoveCore(chess, from, to))
        {
            return false;
        }

        if (from == to)
        {
            return false;
        }

        if (!chess.IsEnemy(this, to, ChessType.帥))
        {
            return false;
        }

        if (!chess.IsPosValid(from) || !chess.IsPosValid_Abs(ChessType.帥, to))
        {
            return false;
        }

        //AppUtils.AssertDataValidation(this.GetChessData(from.Row, from.Column) == chess, $"{chess.Type}不在{from.ToString()}");

        var targetData = this.GetChessData(to.Row, to.Column);

        bool prevent = chess.IsEmpty || (!targetData.IsEmpty && chess.IsRed == targetData.IsRed);

        if (prevent)
        {
            return false;
        }

        if (this.SelfRescue(chess, to) || chess.IsDangerous(_canEatVisitor, from, out _))
        {
            return false;
        }

        return true;
    }

    protected override void DisposeCore()
    {
        _canEatVisitor = null;

        base.DisposeCore();
    }
}
