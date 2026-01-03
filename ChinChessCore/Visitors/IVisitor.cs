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

        bool Visit(ChinChessJu chess, Position from, Position to);
        bool Visit(ChinChessMa chess, Position from, Position to);
        bool Visit(ChinChessPao chess, Position from, Position to);
        bool Visit(ChinChessBing chess, Position from, Position to);

        bool Visit(ChinChessXiang chess, Position from, Position to);
        bool Visit(ChinChessShi chess, Position from, Position to);
        bool Visit(ChinChessShuai chess, Position from, Position to);
    }
}