using ChinChessCore.Visitors;
using ChinChessCore.Contracts;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Utils;
using System.Diagnostics;
using System;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessCore.Models
{
    [DebuggerDisplay("IsRed={Data.IsRed}, Type={Data.Type}")]
    public class ChinChessModel : NotifyBase
    {
        public int Row => this.Pos.Row;
        public int Column => this.Pos.Column;

        /// <summary>
        /// 空棋子
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public ChinChessModel(int row, int column)
        {
            this.Pos = new Position(row, column);

            this.Data = InnerChinChess.Empty;
        }

        /// <summary>
        /// 标准棋子
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="isJieQi"></param>
        public ChinChessModel(int row, int column, bool isJieQi)
        {
            this.Pos = new Position(row, column);

            this.InitData(row, column, isJieQi);
        }

        public bool Reload(ChinChessInfo chessInfo)
        {
            var chessType = chessInfo.ChessType;
            var isRed = chessInfo.IsRed;
            var pos = chessInfo.Pos;

            InnerChinChess temp = InnerChinChess.Empty;

            switch (chessType)
            {
                case ChessType.炮:
                    temp = new ChinChessPao(isRed);
                    break;
                case ChessType.兵:
                    temp = new ChinChessBing(isRed);
                    break;
                case ChessType.車:
                    temp = new ChinChessJu(isRed);
                    break;
                case ChessType.馬:
                    temp = new ChinChessMa(isRed);
                    break;
                case ChessType.相:
                    temp = new ChinChessXiang(isRed);
                    break;
                case ChessType.仕:
                    temp = new ChinChessShi(isRed);
                    break;
                case ChessType.帥:
                    temp = new ChinChessShuai(isRed);
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }

            AppUtils.AssertOperationValidation(pos == this.Pos, $"{chessInfo}不应该放在{pos}");

            AppUtils.AssertDataValidation(temp.IsPosValid(pos), $"{chessType}{isRed}被放置的位置不合法");

            this.Data = temp;

            return true;
        }

        public Position Pos { get; }

        private InnerChinChess _data;
        public InnerChinChess Data
        {
            get => _data;
            set
            {
                if (SetProperty(ref _data, value.AssertArgumentNotNull(nameof(Data))))
                {
                    if (!_data.IsEmpty)
                    {
                        _data.CurPos = Pos;
                    }
                }
            }
        }

        /// <summary>
        /// 揭棋专用
        /// </summary>
        private InnerChinChess _originData;
        public InnerChinChess OriginData => _originData;

        /// <summary>
        /// 揭棋翻开
        /// </summary>
        public void FlipChess(InnerChinChess realData)
        {
            _originData = this.Data;

            this.Data = realData.AssertArgumentNotNull(nameof(realData));

            realData.OriginPos = this.Pos;
        }

        /// <summary>
        /// 试走棋
        /// </summary>
        /// <param name="newData"></param>
        public void SetDataWithoutNotify(InnerChinChess newData)
        {
            this._data = newData.AssertArgumentNotNull(nameof(newData));

            if (!_data.IsEmpty)
            {
                _data.CurPos = Pos;
            }
        }

        private bool _isDangerous;
        public bool IsDangerous
        {
            get => _isDangerous;
            set => SetProperty<bool>(ref _isDangerous, value);
        }

        private bool _isReadyToPut;
        public bool IsReadyToPut
        {
            get => _isReadyToPut;
            set => SetProperty(ref _isReadyToPut, value);
        }

        public bool TrySelect(IPreMoveVisitor preMoveVisitor)
            => this.Data.PreMove(preMoveVisitor);

        protected override void DisposeCore()
        {
            this._originData = null;

            _data.Dispose();
            _data = null;

            base.DisposeCore();
        }

        private void InitData(int row, int column, bool isJieQi)
        {
            AppUtils.Assert(new Position(row, column).IsValid, "超出范围");

            bool isRed = row > 4;

            if (row == 0 || row == 9)
            {
                if (column == 0 || column == 8)
                {
                    Data = new ChinChessJu(isRed, isJieQi, isJieQi);
                    return;
                }
                else if (column == 1 || column == 7)
                {
                    Data = new ChinChessMa(isRed, isJieQi, isJieQi);
                    return;
                }
                else if (column == 2 || column == 6)
                {
                    Data = new ChinChessXiang(isRed, isJieQi, isJieQi);
                    return;
                }
                else if (column == 3 || column == 5)
                {
                    Data = new ChinChessShi(isRed, isJieQi, isJieQi);
                    return;
                }
                else if (column == 4)
                {
                    Data = new ChinChessShuai(isRed, isJieQi);
                    return;
                }
            }
            else if (row == 3 || row == 6)
            {
                if (column == 0 || column == 2 || column == 4 || column == 6 || column == 8)
                {
                    Data = new ChinChessBing(isRed, isJieQi, isJieQi);
                    return;
                }
            }
            else if (row == 2 || row == 7)
            {
                if (column == 1 || column == 7)
                {
                    Data = new ChinChessPao(isRed, isJieQi, isJieQi);
                    return;
                }
            }

            _data = InnerChinChess.Empty;
        }
    }
}