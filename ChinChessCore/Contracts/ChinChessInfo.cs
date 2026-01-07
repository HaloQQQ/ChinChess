using ChinChessCore.Models;
using IceTea.Pure.Extensions;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ChinChessCore.Contracts
{
    /// <summary>
    /// A(a)57
    /// </summary>
    [DebuggerDisplay("{IsRed}{ChessType}{Pos}")]
    public struct ChinChessInfo
    {
        public ChinChessInfo(Position pos, bool isRed, ChessType chessType)
        {
            Pos = pos;
            IsRed = isRed;
            ChessType = chessType;
        }

        public Position Pos { get; }

        public bool IsRed { get; }

        public ChessType ChessType { get; }

        public bool IsValidPos()
        {
            Position p = this.Pos;
            switch (this.ChessType)
            {
                case ChessType.兵:
                    if (this.IsRed)
                    {
                        return p.Row.IsInRange(0, 4) && p.Column.IsInRange(0, 8)
                                         || p.IsIn(new[] {
                                                new Position(6, 0), new Position(6, 2),
                                                new Position(6, 4), new Position(6, 6),
                                                new Position(6, 8),
                                                new Position(5, 0), new Position(5, 2),
                                                new Position(5, 4), new Position(5, 6),
                                                new Position(5, 8)
                                         });
                    }
                    else
                    {
                        return p.Row.IsInRange(5, 9) && p.Column.IsInRange(0, 8)
                                        || p.IsIn(new[] {
                                                new Position(3, 0), new Position(3, 2),
                                                new Position(3, 4), new Position(3, 6),
                                                new Position(3, 8),
                                                new Position(4, 0), new Position(4, 2),
                                                new Position(4, 4), new Position(4, 6),
                                                new Position(4, 8) });
                    }
                case ChessType.相:
                    if (this.IsRed)
                    {
                        return p.IsIn(new[] {
                                            new Position(9, 2), new Position(9, 6),
                                            new Position(7, 0), new Position(7, 4), new Position(7, 8),
                                            new Position(5, 2), new Position(5, 6)});
                    }
                    else
                    {
                        return p.IsIn(new[] {
                                            new Position(0, 2), new Position(0, 6),
                                            new Position(2, 0), new Position(2, 4), new Position(2, 8),
                                            new Position(4, 2), new Position(4, 6)});
                    }
                case ChessType.仕:
                    if (this.IsRed)
                    {
                        return p.IsIn(new[] {
                                            new Position(9, 3), new Position(9, 5),
                                            new Position(8, 4),
                                            new Position(7, 3), new Position(7, 5)});
                    }
                    else
                    {
                        return p.IsIn(new[] {
                                            new Position(0, 3), new Position(0, 5),
                                            new Position(1, 4),
                                            new Position(2, 3), new Position(2, 5)});
                    }
                case ChessType.帥:
                    if (this.IsRed)
                    {
                        return p.Row.IsInRange(7, 9) && p.Column.IsInRange(3, 5);
                    }
                    else
                    {
                        return p.Row.IsInRange(0, 2) && p.Column.IsInRange(3, 5);
                    }
                default:
                    return p.Row.IsInRange(0, 9) && p.Column.IsInRange(0, 8);
            }
        }

        public static ChinChessInfo Load(string info)
        {
            var isValid = Regex.IsMatch(info, "[A-Ga-g][0-9][0-8]");

            if (!isValid)
            {
                throw new InvalidOperationException("数据格式不合要求");
            }

            char chessTypeStr = info[0];
            var row = info[1].ToString();
            var column = info[2].ToString();

            bool isRed = false;
            ChessType chessType = default;

            switch (chessTypeStr)
            {
                case 'A':
                    isRed = false;
                    chessType = ChessType.炮;
                    break;
                case 'B':
                    isRed = false;
                    chessType = ChessType.兵;
                    break;
                case 'C':
                    isRed = false;
                    chessType = ChessType.車;
                    break;
                case 'D':
                    isRed = false;
                    chessType = ChessType.馬;
                    break;
                case 'E':
                    isRed = false;
                    chessType = ChessType.相;
                    break;
                case 'F':
                    isRed = false;
                    chessType = ChessType.仕;
                    break;
                case 'G':
                    isRed = false;
                    chessType = ChessType.帥;
                    break;

                case 'a':
                    isRed = true;
                    chessType = ChessType.炮;
                    break;
                case 'b':
                    isRed = true;
                    chessType = ChessType.兵;
                    break;
                case 'c':
                    isRed = true;
                    chessType = ChessType.車;
                    break;
                case 'd':
                    isRed = true;
                    chessType = ChessType.馬;
                    break;
                case 'e':
                    isRed = true;
                    chessType = ChessType.相;
                    break;
                case 'f':
                    isRed = true;
                    chessType = ChessType.仕;
                    break;
                case 'g':
                    isRed = true;
                    chessType = ChessType.帥;
                    break;

                default:
                    break;
            }

            return new ChinChessInfo(
                    new Position(
                        Convert.ToInt32(row),
                        Convert.ToInt32(column)
                    ),
                    isRed,
                    chessType
                );
        }

        public override string ToString()
        {
            char c = default;
            switch (ChessType)
            {
                case ChessType.炮:
                    c = 'A';
                    break;
                case ChessType.兵:
                    c = 'B';
                    break;
                case ChessType.車:
                    c = 'C';
                    break;
                case ChessType.馬:
                    c = 'D';
                    break;
                case ChessType.相:
                    c = 'E';
                    break;
                case ChessType.仕:
                    c = 'F';
                    break;
                case ChessType.帥:
                    c = 'G';
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }

            if (this.IsRed)
            {
                c = (char)(c + 32);
            }

            return $"{c}{Pos.Row}{Pos.Column}";
        }
    }
}
