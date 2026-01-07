using ChinChessCore.Models;
using System;
using System.Collections.Generic;

namespace ChinChessCore.Visitors
{
    public interface IVisitor : IDisposable
    {
        bool FaceToFace();

        ChinChessModel GetChess(Position pos);

        InnerChinChess GetChessData(Position pos);

        IEnumerable<ChinChessModel> GetChesses();

        bool Visit(ChinChessJu chess, Position to);
        bool Visit(ChinChessMa chess, Position to);
        bool Visit(ChinChessPao chess, Position to);
        bool Visit(ChinChessBing chess, Position to);

        bool Visit(ChinChessXiang chess, Position to);
        bool Visit(ChinChessShi chess, Position to);
        bool Visit(ChinChessShuai chess, Position to);
    }
}