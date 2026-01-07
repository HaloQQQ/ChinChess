using ChinChessCore.Contracts;
using ChinChessCore.Visitors;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Utils;
using System;
using System.Diagnostics;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessCore.Models
{
    [DebuggerDisplay("{ToolTip}")]
    public class InnerChinChess : NotifyBase, IChineseChess
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

        /// <summary>
        /// 只能由<see cref="ChinChessModel"/>设置
        /// </summary>
        public Position CurPos { get; internal set; }


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

        internal virtual bool IsAllowTo(Position toPos)
        {
            AppUtils.AssertDataValidation(toPos.IsValid, "位置超出允许范围");

            return true;
        }

        #region IChineseChess
        public bool CanPutTo(ICanPutToVisitor canPutToVisitor, Position to)
            => this.Accept(canPutToVisitor, to);

        public bool PreMove(IPreMoveVisitor preMoveVisitor)
            => this.Accept(preMoveVisitor, default);

        /// <summary>
        /// 1、自救
        /// 2、威胁可被击杀
        /// 3、替死
        /// </summary>
        /// <param name="guardVisitor"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool CanBeSaveFromMe(IGuardVisitor guardVisitor, Position to)
            => this.Accept(guardVisitor, to);

        public bool IsDangerous(ICanPutToVisitor canPutToVisitor, out ChinChessModel killer)
        {
            if (this.IsEmpty)
            {
                killer = default;

                return false;
            }

            Position selfPos = this.CurPos;
            int toRow = selfPos.Row, toColumn = selfPos.Column;

            #region 兵
            foreach (var item in new[] {
                                new Position(toRow - 1, toColumn),
                                new Position(toRow + 1, toColumn),
                                new Position(toRow, toColumn - 1),
                                new Position(toRow, toColumn + 1)
                            })
            {
                if (TryPutFromIfEnemy(canPutToVisitor, item, ChessType.兵))
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
                if (TryPutFromIfEnemy(canPutToVisitor, item, ChessType.馬))
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
                if (TryPutFromIfEnemy(canPutToVisitor, item, ChessType.相))
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
                if (TryPutFromIfEnemy(canPutToVisitor, item, ChessType.仕))
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
                if (TryPutFromIfEnemy(canPutToVisitor, item, ChessType.帥))
                {
                    killer = canPutToVisitor.GetChess(item);

                    return true;
                }
            }
            #endregion

            killer = default;

            return false;
        }

        /// <summary>
        /// 离开此地，到不危险的地方
        /// </summary>
        /// <param name="canPutToVisitor"></param>
        /// <param name="leaveInHorizontal">离开方向是水平方向？null则随便都可以；这个给炮架使用</param>
        /// <param name="from"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual bool CanLeave(ICanPutToVisitor canPutToVisitor, bool? leaveInHorizontal = null)
            => throw new NotImplementedException();

        /// <summary>
        /// chessType是否为敌方<see cref="chessType"/>, 并且可以from=>cur
        /// </summary>
        /// <param name="canPutToVisitor"></param>
        /// <param name="from"></param>
        /// <param name="chessType"></param>
        /// <returns></returns>
        public bool TryPutFromIfEnemy(ICanPutToVisitor canPutToVisitor, Position from, ChessType chessType)
        {
            if (IsEnemy(canPutToVisitor, from, chessType))
            {
                var data = canPutToVisitor.GetChessData(from);

                return data.CanPutTo(canPutToVisitor, this.CurPos);
            }

            return false;
        }

        /// <summary>
        /// position是否为敌方<see cref="chessType"/>中的一个
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="position"></param>
        /// <param name="chessType"></param>
        /// <returns></returns>
        public bool IsEnemy(IVisitor visitor, Position position, ChessType chessType)
        {
            if (!IsPosValid(chessType, position, true))
            {
                return false;
            }

            var data = visitor.GetChessData(position);

            return (this.IsEnemy(data) == true) && ((data.Type & chessType) == data.Type);
        }

        /// <summary>
        /// 是否为敌军
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool? IsEnemy(InnerChinChess target)
        {
            if (this.IsEmpty || target.IsEmpty)
            {
                return null;
            }

            return this.IsRed != target.IsRed;
        }
        #endregion

        /// <summary>
        /// 检查自身是否可放置到pos
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsPosValid(Position pos)
            => this.IsPosValid((ChessType)this.Type, pos, false);

        /// <summary>
        /// 检查isEnemy方的chessType是否可以放置在pos
        /// </summary>
        /// <param name="chessType"></param>
        /// <param name="pos"></param>
        /// <param name="isEnemy"></param>
        /// <returns></returns>
        public bool IsPosValid(ChessType chessType, Position pos, bool isEnemy = true)
        {
            if (chessType != ChessType.帥 && this.IsJieQi && !this.IsBack)
            {
                return pos.IsValid;
            }

            bool isRed = isEnemy ? !(bool)this.IsRed : (bool)this.IsRed;

            return new ChinChessInfo(pos, isRed, chessType).IsValidPos();
        }

        public virtual bool Accept(IVisitor visitor, Position to) => throw new NotImplementedException();

        public string ToolTip => this.ToString();

        public override string ToString()
            => $"Pos={CurPos}, IsRed={IsRed}, Type={Type}, IsBack={IsBack}, IsJieQi={IsJieQi}, HasNotUsed={HasNotUsed}";
    }
}
