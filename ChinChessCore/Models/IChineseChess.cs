using ChinChessCore.Visitors;

namespace ChinChessCore.Models
{
    public interface IChineseChess
    {
        /// <summary>
        /// 尝试选中棋子
        /// </summary>
        /// <returns></returns>
        bool TrySelect(IPreMoveVisitor canEatVisitor);
    }
}