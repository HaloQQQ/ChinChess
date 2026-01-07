using ChinChessCore.Visitors;

namespace ChinChessCore.Models
{
    public interface IChineseChess
    {
        bool CanPutTo(ICanPutToVisitor canPutToVisitor, Position to);

        bool PreMove(IPreMoveVisitor preMoveVisitor);

        bool CanBeSaveFromMe(IGuardVisitor guardVisitor, Position to);

        bool IsDangerous(ICanPutToVisitor canPutToVisitor, out ChinChessModel killer);

        bool CanLeave(ICanPutToVisitor canPutToVisitor, bool? leaveInHorizontal = null);

        bool TryPutFromIfEnemy(ICanPutToVisitor canPutToVisitor, Position from, ChessType chessType);

        bool IsEnemy(IVisitor visitor, Position position, ChessType chessType);

        bool? IsEnemy(InnerChinChess target);
    }
}