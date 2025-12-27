using ChinChessCore.Models;
using System;

namespace ChinChessCore.Contracts
{
    internal static class Constants
    {
        public const string MovePattern =
            @"^((([一二]?[车車馬马炮兵卒相象仕士]|[三四五]?[兵卒]|[帥帅将])([一二三四五六七八九][进退平][一二三四五六七八九]))|(([１２12]?[车車馬马炮兵卒相象仕士]|[３４５3-5]?[兵卒]|[帥帅将])([１２３４５６７８９1-9][进退平][１２３４５６７８９1-9])))$";

        public static ChessType ConvertChessCharToType(char chessTypeChar)
        {
            switch (chessTypeChar)
            {
                case '车':
                case '車':
                    return ChessType.車;
                case '马':
                case '馬':
                    return ChessType.馬;
                case '炮':
                    return ChessType.炮;
                case '兵':
                case '卒':
                    return ChessType.兵;
                case '相':
                case '象':
                    return ChessType.相;
                case '士':
                case '仕':
                    return ChessType.仕;
                case '帅':
                case '将':
                case '帥':
                    return ChessType.帥;
                default:
                    throw new ArgumentException($"未知的棋子类型: {chessTypeChar}");
            }
        }

        public static char NumToChar(int num, bool isRed)
        {
            switch (num)
            {
                case 1:
                    return isRed ? '一' : '1';
                case 2:
                    return isRed ? '二' : '2';
                case 3:
                    return isRed ? '三' : '3';
                case 4:
                    return isRed ? '四' : '4';
                case 5:
                    return isRed ? '五' : '5';
                case 6:
                    return isRed ? '六' : '6';
                case 7:
                    return isRed ? '七' : '7';
                case 8:
                    return isRed ? '八' : '8';
                case 9:
                    return isRed ? '九' : '9';
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static int CharToNum(char numChar)
        {
            switch (numChar)
            {
                case '１':
                case '1':
                case '一':
                    return 1;
                case '２':
                case '2':
                case '二':
                    return 2;
                case '３':
                case '3':
                case '三':
                    return 3;
                case '４':
                case '4':
                case '四':
                    return 4;
                case '５':
                case '5':
                case '五':
                    return 5;
                case '６':
                case '6':
                case '六':
                    return 6;
                case '７':
                case '7':
                case '七':
                    return 7;
                case '８':
                case '8':
                case '八':
                    return 8;
                case '９':
                case '9':
                case '九':
                    return 9;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 行或列的索引
        /// </summary>
        /// <param name="numChar"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int GetRowOrColumnFromChar(char numChar)
        {
            switch (numChar)
            {
                case '１':
                case '1':
                    return 0;
                case '一':
                    return 8;
                case '２':
                case '2':
                    return 1;
                case '二':
                    return 7;
                case '３':
                case '3':
                    return 2;
                case '三':
                    return 6;
                case '４':
                case '4':
                    return 3;
                case '四':
                    return 5;
                case '５':
                case '5':
                    return 4;
                case '五':
                    return 4;
                case '６':
                case '6':
                    return 5;
                case '六':
                    return 3;
                case '７':
                case '7':
                    return 6;
                case '七':
                    return 2;
                case '８':
                case '8':
                    return 7;
                case '八':
                    return 1;
                case '９':
                case '9':
                    return 8;
                case '九':
                    return 0;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static string ColumnToString(int column, bool isRed)
        {
            if (!isRed)
            {
                return (column + 1).ToString();
            }

            switch (column)
            {
                case 0: return "九";
                case 1: return "八";
                case 2: return "七";
                case 3: return "六";
                case 4: return "五";
                case 5: return "四";
                case 6: return "三";
                case 7: return "二";
                case 8: return "一";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
