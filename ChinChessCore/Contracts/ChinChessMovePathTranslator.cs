using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChinChessCore.Contracts
{
    /// <summary>
    /// 中国象棋术语解析工具类
    /// 用于解析标准的象棋行棋术语，如"炮二平五"、"马八进七"等
    /// </summary>
    public static class ChinChessMovePathTranslator
    {
        /// <summary>
        /// 解析象棋术语并返回移动路径
        /// </summary>
        /// <param name="notation">象棋术语，如"炮二平五"</param>
        /// <param name="isRed">是否为红方棋子</param>
        /// <param name="currentBoard">当前棋盘状态，用于处理多个相同棋子的情况</param>
        /// <returns>移动路径</returns>
        public static MovePath FromNotation(string notation, IList<ChinChessModel> currentBoard)
        {
            notation = notation.AssertArgumentNotNull(nameof(notation)).Trim().Replace(" ", "");
            AppUtils.AssertDataValidation(Regex.IsMatch(notation, Constants.MovePattern), "术语格式不正确");

            int nthChess = 1;
            int notaionLength = notation.Length;

            if (notaionLength > 4)
            {
                nthChess = Constants.CharToNum(notation[notaionLength - 5]);
            }

            ChessType chessType = Constants.ConvertChessCharToType(notation[notaionLength - 4]);

            char fromChar = notation[notaionLength - 3];
            bool isRed = Regex.IsMatch(fromChar.ToString(), "[一二三四五六七八九]");

            int fromColumn = Constants.GetRowOrColumnFromChar(fromChar);

            // 计算起始位置和目标位置
            var fromPosition = CalculateStartPosition(nthChess, chessType, fromColumn, isRed, currentBoard);

            AppUtils.AssertDataValidation(Enum.TryParse<EnumMoveDirection>(notation[notaionLength - 2].ToString(), out EnumMoveDirection direction), "数据格式不对");
            char targetChar = notation[notaionLength - 1];
            var toPosition = CalculateTargetPosition(chessType, fromPosition, direction, targetChar, isRed);

            return new MovePath(fromPosition, toPosition);

            Position CalculateStartPosition(int __indexChess, ChessType __chessType, int __fromColumn, bool ___isRed, IList<ChinChessModel> __currentBoard)
            {
                AppUtils.AssertDataValidation(__fromColumn.IsInRange(0, Position.MaxColumnIndex), "起始列号超出范围");

                AppUtils.AssertDataValidation(!__currentBoard.IsNullOrEmpty(), "未提供棋盘数据");

                // 查找符合条件的所有棋子：相同类型、相同颜色、在指定列上
                var candidates = __currentBoard.Where(chess =>
                    chess.Data.Type == __chessType &&
                    chess.Data.IsRed == ___isRed &&
                    chess.Pos.Column == __fromColumn
                ).ToList();

                AppUtils.AssertDataValidation(!candidates.IsNullOrEmpty(), "目标数据不存在");

                if (candidates.Count == 1)
                {
                    // 如果只有一个符合条件的棋子，直接返回其位置
                    return candidates[0].Pos;
                }
                else
                {
                    var orderedPieces = ___isRed ?
                            candidates.OrderBy(c => c.Pos.Row).ToList() :
                            candidates.OrderByDescending(c => c.Pos.Row).ToList();

                    return orderedPieces[__indexChess - 1].Pos;
                }
            }

            Position CalculateTargetPosition(ChessType __chessType, Position from, EnumMoveDirection __direction, char __targetChar, bool __isRed)
            {
                int targetRow = from.Row;
                int targetColumn = from.Column;

                switch (__direction)
                {
                    case EnumMoveDirection.进: // 前进
                        if (__chessType == ChessType.相)
                        {
                            targetColumn = Constants.GetRowOrColumnFromChar(__targetChar);
                            targetRow = __isRed ? (from.Row - 2) : (from.Row + 2);
                        }
                        else if (__chessType == ChessType.仕)
                        {
                            targetColumn = Constants.GetRowOrColumnFromChar(__targetChar);
                            targetRow = __isRed ? (from.Row - 1) : (from.Row + 1);
                        }
                        else if (__chessType == ChessType.馬)
                        {
                            targetColumn = Constants.GetRowOrColumnFromChar(__targetChar);

                            int diff = Math.Abs(targetColumn - from.Column) == 1 ? 2 : 1;

                            targetRow = __isRed ? (from.Row - diff) : (from.Row + diff);
                        }
                        else
                        {
                            int num = Constants.CharToNum(__targetChar);
                            targetRow = __isRed ? (from.Row - num) : (from.Row + num);
                        }
                        break;

                    case EnumMoveDirection.退: // 后退
                        if (__chessType == ChessType.相)
                        {
                            targetColumn = Constants.GetRowOrColumnFromChar(__targetChar);
                            targetRow = __isRed ? (from.Row + 2) : (from.Row - 2);
                        }
                        else if (__chessType == ChessType.仕)
                        {
                            targetColumn = Constants.GetRowOrColumnFromChar(__targetChar);
                            targetRow = __isRed ? (from.Row + 1) : (from.Row - 1);
                        }
                        else if (__chessType == ChessType.馬)
                        {
                            targetColumn = Constants.GetRowOrColumnFromChar(__targetChar);

                            int diff = Math.Abs(targetColumn - from.Column) == 1 ? 2 : 1;

                            targetRow = __isRed ? (from.Row + diff) : (from.Row - diff);
                        }
                        else
                        {
                            int num = Constants.CharToNum(__targetChar);

                            targetRow = __isRed ? (from.Row + num) : (from.Row - num);
                        }
                        break;

                    case EnumMoveDirection.平: // 平移
                        targetColumn = Constants.GetRowOrColumnFromChar(__targetChar);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }

                var targetPos = new Position(targetRow, targetColumn);
                // 验证位置是否在棋盘范围内
                AppUtils.AssertDataValidation(targetPos.IsValid, "目标位置超出棋盘范围");

                return targetPos;
            }
        }

        /// <summary>
        /// [一二三四五12345]?兵[二2]进二
        /// </summary>
        /// <param name="movePath">移动路径</param>
        /// <param name="chessType">棋子类型</param>
        /// <param name="isRed">是否为红方</param>
        /// <param name="currentBoard">如果起始列有多个本方该类型棋子，则需要传入当前棋盘状态</param>
        /// <returns>象棋术语</returns>
        public static string ToNotation(MovePath movePath, ChessType chessType, bool isRed, IList<ChinChessModel> currentBoard)
        {
            var from = movePath.From;
            var to = movePath.To;

            // 获取棋子字符
            string chessChar = chessType.ToString();

            // 计算方向和目标信息
            string direction = string.Empty;
            string targetChar = string.Empty;

            if (from.Row == to.Row)
            {
                // 平移
                direction = EnumMoveDirection.平.ToString();
                targetChar = Constants.ColumnToString(to.Column, isRed);
            }
            else
            {
                int rowDiff = to.Row - from.Row;

                bool isForward = (isRed && rowDiff < 0) || (!isRed && rowDiff > 0);

                direction = isForward ? EnumMoveDirection.进.ToString() : EnumMoveDirection.退.ToString();

                if (chessType == ChessType.相 || chessType == ChessType.馬 || chessType == ChessType.仕)
                {
                    targetChar = Constants.ColumnToString(to.Column, isRed);
                }
                else
                {
                    int steps = Math.Abs(rowDiff);
                    targetChar = Constants.NumToChar(steps, isRed).ToString();
                }
            }

            string positionPrefix = string.Empty;

            // 处理多个相同棋子的情况
            var sameColumnPieces = currentBoard.Where(c =>
                c.Data.Type == chessType &&
                c.Data.IsRed == isRed &&
                c.Pos.Column == from.Column
            ).ToList();

            if (sameColumnPieces.Count > 1)
            {
                // 如果有多个相同棋子在同一列，需要区分前后
                var orderedPieces = isRed ?
                    sameColumnPieces.OrderBy(c => c.Pos.Row).ToList() :
                    sameColumnPieces.OrderByDescending(c => c.Pos.Row).ToList();

                int index = orderedPieces.FindIndex(c => c.Pos.Equals(from));
                positionPrefix = Constants.NumToChar(index + 1, isRed).ToString();
            }

            var result = $"{positionPrefix}{chessChar}{Constants.ColumnToString(from.Column, isRed)}{direction}{targetChar}";

            AppUtils.AssertDataValidation(Regex.IsMatch(result, Constants.MovePattern), "术语格式不正确");

            return result;
        }
    }
}
