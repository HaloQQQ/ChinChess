using ChinChessClient.Commands;
using ChinChessClient.Visitors;
using ChinChessCore.Models;

namespace ChinChessClient.Models;

internal interface IChineseChess
{
    /// <summary>
    /// 尝试放置
    /// </summary>
    /// <returns></returns>
    bool TryPutTo(IVisitor canEatVisitor, Position to, IList<IChinChessCommand> commandStack, Action<string> publishMsg);

    /// <summary>
    /// 尝试选中棋子
    /// </summary>
    /// <returns></returns>
    bool TrySelect(IVisitor canEatVisitor);
}
