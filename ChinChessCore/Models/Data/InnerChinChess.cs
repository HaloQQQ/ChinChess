using ChinChessCore.Visitors;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Extensions;
using System;
using System.Diagnostics;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessCore.Models
{
    [DebuggerDisplay("{ToolTip}")]
    public class InnerChinChess : NotifyBase
    {
        public static InnerChinChess Empty = new InnerChinChess();

        private InnerChinChess() { }

        public InnerChinChess(bool isRed, ChessType type, bool isJieQi = false, bool isBack = false)
        {
            IsRed = isRed;
            Type = type;

            IsBack = isBack;
            IsJieQi = isJieQi;
        }

        public bool IsBack { get; }
        public bool IsJieQi { get; }

        public ChessType? Type { get; }
        public bool IsEmpty => Type == null;

        public bool? IsRed { get; }

        private Position _originPos = new Position(-1, -1);
        public Position OriginPos
        {
            get => _originPos;
            set => _originPos = value;
        }

        private bool _hasNotUsed;
        public bool HasNotUsed
        {
            get => _hasNotUsed;
            set => SetProperty<bool>(ref _hasNotUsed, value);
        }

        #region IChineseChess
        public bool CanPutTo(ICanPutToVisitor canPutToVisitor, Position from, Position to)
            => this.Accept(canPutToVisitor, from, to);

        public bool PreMove(IPreMoveVisitor preMoveVisitor, Position from)
            => this.Accept(preMoveVisitor, from, default);

        /// <summary>
        /// 1、威胁可被击杀
        /// 2、替死
        /// </summary>
        /// <param name="guardVisitor"></param>
        /// <param name="killerPos"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool CanBeProtected(IGuardVisitor guardVisitor, Position killerPos, Position to)
            => this.Accept(guardVisitor, killerPos, to);

        public bool IsDangerous(ICanPutToVisitor canPutToVisitor, Position selfPos, out ChinChessModel killer)
        {
            if (this.IsEmpty)
            {
                killer = default;

                return false;
            }

            int toRow = selfPos.Row, toColumn = selfPos.Column;

            #region 兵
            foreach (var item in new[] {
                                new Position(toRow - 1, toColumn),
                                new Position(toRow + 1, toColumn),
                                new Position(toRow, toColumn - 1),
                                new Position(toRow, toColumn + 1)
                            })
            {
                if (TryPutToIfIsEnemy(canPutToVisitor, item, selfPos, ChessType.兵))
                {
                    killer = canPutToVisitor.GetChess(item);

                    return true;
                }
            }
            #endregion

            #region 马
            foreach (var item in new[] {
                                new Position(toRow - 2, toColumn - 1),
                                new Position(toRow - 1, toColumn - 2),
                                new Position(toRow - 2, toColumn + 1),
                                new Position(toRow - 1, toColumn + 2),
                                new Position(toRow + 2, toColumn - 1),
                                new Position(toRow + 1, toColumn - 2),
                                new Position(toRow + 2, toColumn + 1),
                                new Position(toRow + 1, toColumn + 2)
                            })
            {
                if (TryPutToIfIsEnemy(canPutToVisitor, item, selfPos, ChessType.馬))
                {
                    killer = canPutToVisitor.GetChess(item);

                    return true;
                }
            }
            #endregion

            #region 車
            foreach ((int rowStep, int columnStep) in new Tuple<int, int>[] {
                                    new Tuple<int, int>(-1,0),
                                    new Tuple<int, int>(1,0),
                                    new Tuple<int, int>(0,-1),
                                    new Tuple<int, int>(0,1)
                                }
                    )
            {
                if (FindJu(canPutToVisitor, toRow, toColumn, rowStep, columnStep, out Position pos))
                {
                    killer = canPutToVisitor.GetChess(pos);

                    return true;
                }
            }

            bool FindJu(IVisitor canEatVisitor, int row, int column, int rowStep, int columnStep, out Position pos)
            {
                int fromRow = row + rowStep, fromColumn = column + columnStep;
                Position currentPos = new Position(fromRow, fromColumn);

                while (currentPos.IsValid)
                {
                    if (!canEatVisitor.GetChessData(currentPos).IsEmpty)
                    {
                        if (IsEnemy(canEatVisitor, currentPos, ChessType.車))
                        {
                            pos = currentPos;
                            return true;
                        }

                        break;
                    }

                    currentPos = new Position(fromRow += rowStep, fromColumn += columnStep);
                }

                pos = default;

                return false;
            }
            #endregion

            #region 炮
            foreach ((int rowStep, int columnStep) in new Tuple<int, int>[] {
                                    new Tuple<int, int>(-1, 0),
                                    new Tuple<int, int>(1, 0),
                                    new Tuple<int, int>(0, -1),
                                    new Tuple<int, int>(0, 1)
                                }
                    )
            {
                if (FindPao(canPutToVisitor, toRow, toColumn, rowStep, columnStep, out Position pos))
                {
                    killer = canPutToVisitor.GetChess(pos);

                    return true;
                }
            }

            bool FindPao(IVisitor visitor, int row, int column, int rowStep, int columnStep, out Position pos)
            {
                int fromRow = row + rowStep, fromColumn = column + columnStep;
                Position currentPos = new Position(fromRow, fromColumn);

                int mountainsCount = 0;

                while (currentPos.IsValid)
                {
                    if (!visitor.GetChessData(currentPos).IsEmpty)
                    {
                        mountainsCount++;

                        if (mountainsCount == 2)
                        {
                            if (IsEnemy(visitor, currentPos, ChessType.炮))
                            {
                                pos = currentPos;
                                return true;
                            }

                            break;
                        }
                    }

                    currentPos = new Position(fromRow += rowStep, fromColumn += columnStep);
                }

                pos = default;

                return false;
            }
            #endregion

            #region 相
            foreach (var item in new[] {
                                new Position(toRow - 2, toColumn - 2),
                                new Position(toRow - 2, toColumn + 2),
                                new Position(toRow + 2, toColumn - 2),
                                new Position(toRow + 2, toColumn + 2)
                            })
            {
                if (TryPutToIfIsEnemy(canPutToVisitor, item, selfPos, ChessType.相))
                {
                    killer = canPutToVisitor.GetChess(item);

                    return true;
                }
            }
            #endregion

            #region 仕
            foreach (var item in new[] {
                                new Position(toRow - 1, toColumn - 1),
                                new Position(toRow - 1, toColumn + 1),
                                new Position(toRow + 1, toColumn - 1),
                                new Position(toRow + 1, toColumn + 1)
                            })
            {
                if (TryPutToIfIsEnemy(canPutToVisitor, item, selfPos, ChessType.仕))
                {
                    killer = canPutToVisitor.GetChess(item);

                    return true;
                }
            }
            #endregion

            #region 帥
            foreach (var item in new[] {
                                new Position(toRow, toColumn - 1),
                                new Position(toRow, toColumn + 1),
                                new Position(toRow - 1, toColumn),
                                new Position(toRow + 1, toColumn)
                            })
            {
                if (TryPutToIfIsEnemy(canPutToVisitor, item, selfPos, ChessType.帥))
                {
                    killer = canPutToVisitor.GetChess(item);

                    return true;
                }
            }
            #endregion

            killer = default;

            return false;
        }
        #endregion

        /// <summary>
        /// 离开此地，到不危险的地方
        /// </summary>
        /// <param name="canPutToVisitor"></param>
        /// <param name="from"></param>
        /// <param name="isHorizontal">杀手和被害者是否同一行</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual bool CanLeave(ICanPutToVisitor canPutToVisitor, Position from, bool isHorizontal = true)
            => throw new NotImplementedException();

        /// <summary>
        /// chessType是否为敌方<see cref="chessType"/>, 并且可以from=>to
        /// </summary>
        /// <param name="canPutToVisitor"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="chessType"></param>
        /// <returns></returns>
        public bool TryPutToIfIsEnemy(ICanPutToVisitor canPutToVisitor, Position from, Position to, ChessType chessType)
        {
            if (IsEnemy(canPutToVisitor, from, chessType))
            {
                var data = canPutToVisitor.GetChessData(from);

                if (data.CanPutTo(canPutToVisitor, from, to))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 当前点是否为敌方<see cref="chessType"/>中的一个
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="position"></param>
        /// <param name="chessType"></param>
        /// <returns></returns>
        public bool IsEnemy(IVisitor visitor, Position position, ChessType chessType)
        {
            if (!IsPosValid_Abs(chessType, position, true))
            {
                return false;
            }

            var data = visitor.GetChessData(position);

            return this.IsEnemy(data) && ((data.Type & chessType) == data.Type);
        }

        /// <summary>
        /// 是否为敌军
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsEnemy(InnerChinChess target)
            => !target.IsEmpty && this.IsRed != target.IsRed;

        public bool IsPosValid(Position pos)
            => this.IsPosValid_Abs((ChessType)this.Type, pos, false);

        public bool IsPosValid_Rel(ChessType chessType, Position pos, bool isEnemy = true)
        {
            if (chessType != ChessType.帥 && this.IsJieQi && !this.IsBack)
            {
                return pos.Row.IsInRange(0, 9) && pos.Column.IsInRange(0, 8);
            }

            Predicate<Position> predicate = null;

            switch (chessType)
            {
                case ChessType.兵:
                    if (this.IsRed == isEnemy)
                    {
                        predicate = p => p.Row.IsInRange(3, 9) && p.Column.IsInRange(0, 8);
                    }
                    else
                    {
                        predicate = p => p.Row.IsInRange(0, 6) && p.Column.IsInRange(0, 8);
                    }
                    break;
                case ChessType.相:
                    if (this.IsRed == isEnemy)
                    {
                        predicate = p => p.Row.IsInRange(0, 4) && p.Column.IsInRange(0, 8);
                    }
                    else
                    {
                        predicate = p => p.Row.IsInRange(5, 9) && p.Column.IsInRange(0, 8);
                    }
                    break;
                case ChessType.仕:
                case ChessType.帥:
                    if (this.IsRed == isEnemy)
                    {
                        predicate = p => p.Row.IsInRange(0, 2) && p.Column.IsInRange(3, 5);
                    }
                    else
                    {
                        predicate = p => p.Row.IsInRange(7, 9) && p.Column.IsInRange(3, 5);
                    }
                    break;
                default:
                    predicate = p => p.Row.IsInRange(0, 9) && p.Column.IsInRange(0, 8);
                    break;
            }

            return predicate(pos);
        }

        public bool IsPosValid_Abs(ChessType chessType, Position pos, bool isEnemy = true)
        {
            if (chessType != ChessType.帥 && this.IsJieQi && !this.IsBack)
            {
                return pos.Row.IsInRange(0, 9) && pos.Column.IsInRange(0, 8);
            }

            Predicate<Position> predicate = null;

            switch (chessType)
            {
                case ChessType.兵:
                    if (this.IsRed == isEnemy)
                    {
                        predicate = p => p.Row.IsInRange(5, 9) && p.Column.IsInRange(0, 8)
                                        || p.IsIn(new[] {
                                                new Position(3, 0), new Position(3, 2),
                                                new Position(3, 4), new Position(3, 6),
                                                new Position(3, 8),
                                                new Position(4, 0), new Position(4, 2),
                                                new Position(4, 4), new Position(4, 6),
                                                new Position(4, 8) });
                    }
                    else
                    {
                        predicate = p => p.Row.IsInRange(0, 4) && p.Column.IsInRange(0, 8)
                                         || p.IsIn(new[] {
                                                new Position(6, 0), new Position(6, 2),
                                                new Position(6, 4), new Position(6, 6),
                                                new Position(6, 8),
                                                new Position(5, 0), new Position(5, 2),
                                                new Position(5, 4), new Position(5, 6),
                                                new Position(5, 8) });
                    }
                    break;
                case ChessType.相:
                    if (this.IsRed == isEnemy)
                    {
                        predicate = p => p.IsIn(new[] {
                                            new Position(0, 2), new Position(0, 6),
                                            new Position(2, 0), new Position(2, 4), new Position(2, 8),
                                            new Position(4, 2), new Position(4, 6)});
                    }
                    else
                    {
                        predicate = p => p.IsIn(new[] {
                                            new Position(9, 2), new Position(9, 6),
                                            new Position(7, 0), new Position(7, 4), new Position(7, 8),
                                            new Position(5, 2), new Position(5, 6)});
                    }
                    break;
                case ChessType.仕:
                    if (this.IsRed == isEnemy)
                    {
                        predicate = p => p.IsIn(new[] {
                                            new Position(0, 3), new Position(0, 5),
                                            new Position(1, 4),
                                            new Position(2, 3), new Position(2, 5)});
                    }
                    else
                    {
                        predicate = p => p.IsIn(new[] {
                                            new Position(9, 3), new Position(9, 5),
                                            new Position(8, 4),
                                            new Position(7, 3), new Position(7, 5)});
                    }
                    break;
                case ChessType.帥:
                    if (this.IsRed == isEnemy)
                    {
                        predicate = p => p.Row.IsInRange(0, 2) && p.Column.IsInRange(3, 5);
                    }
                    else
                    {
                        predicate = p => p.Row.IsInRange(7, 9) && p.Column.IsInRange(3, 5);
                    }
                    break;
                default:
                    predicate = p => p.Row.IsInRange(0, 9) && p.Column.IsInRange(0, 8);
                    break;
            }

            return predicate(pos);
        }

        public virtual bool Accept(IVisitor visitor, Position from, Position to) => throw new NotImplementedException();

        public string ToolTip => this.ToString();

        public override string ToString()
            => $"IsRed={IsRed}, Type={Type}, IsBack={IsBack}, IsJieQi={IsJieQi}, HasNotUsed={HasNotUsed}";
    }
}
