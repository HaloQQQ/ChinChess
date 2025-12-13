using ChinChessCore.Models;
using System;
using System.Text.RegularExpressions;

namespace ChinChessCore.Contracts
{
    /// <summary>
    /// A(a)57
    /// </summary>
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
