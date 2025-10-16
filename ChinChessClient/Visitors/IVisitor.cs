using ChinChessClient.Models;
using ChinChessCore.Models;

namespace ChinChessClient.Visitors;

internal interface IVisitor : IDisposable
{
    ChinChessModel GetChess(int row, int column);

    InnerChinChess GetChessData(int row, int column);

    IEnumerable<ChinChessModel> GetChesses();

    bool Visit(ChinChessJu chess, Position from, Position to);
    bool Visit(ChinChessMa chess, Position from, Position to);
    bool Visit(ChinChessPao chess, Position from, Position to);
    bool Visit(ChinChessBing chess, Position from, Position to);

    bool Visit(ChinChessXiang chess, Position from, Position to);
    bool Visit(ChinChessShi chess, Position from, Position to);
    bool Visit(ChinChessShuai chess, Position from, Position to);
}
