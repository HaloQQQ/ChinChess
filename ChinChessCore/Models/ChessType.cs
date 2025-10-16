using System;

namespace ChinChessCore.Models
{
    [Flags]
    public enum ChessType : byte
    {
        炮 = 1,
        兵 = 2,
        車 = 4,
        馬 = 8,
        相 = 16,
        仕 = 32,
        帥 = 64
    }
}