using ChinChessClient.Visitors;

namespace ChinChessClient.Models;

internal interface IChineseChess
{
    /// <summary>
    /// 尝试选中棋子
    /// </summary>
    /// <returns></returns>
    bool TrySelect(IVisitor canEatVisitor);
}
