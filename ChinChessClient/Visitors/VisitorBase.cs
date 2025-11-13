using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.Visitors;

internal abstract class VisitorBase : DisposableBase, IVisitor
{
    private IList<ChinChessModel> _datas;

    public VisitorBase(IList<ChinChessModel> datas)
    {
        _datas = datas.AssertNotNull(nameof(IList<ChinChessModel>));
    }

    public IEnumerable<ChinChessModel> GetChesses() => _datas;

    public ChinChessModel GetChess(int row, int column)
    {
        AppUtils.AssertDataValidation(
            row.IsInRange(0, 9) && column.IsInRange(0, 8)
            , "行列超出范围");

        return _datas[row * 9 + column];
    }

    public ChinChessModel GetChess(Position pos) => GetChess(pos.Row, pos.Column);

    public InnerChinChess GetChessData(int row, int column) => GetChess(row, column).Data;

    public InnerChinChess GetChessData(Position pos) => GetChessData(pos.Row, pos.Column);

    public abstract bool Visit(ChinChessJu chess, Position from, Position to);
    public abstract bool Visit(ChinChessMa chess, Position from, Position to);
    public abstract bool Visit(ChinChessPao chess, Position from, Position to);
    public abstract bool Visit(ChinChessBing chess, Position from, Position to);
    public abstract bool Visit(ChinChessXiang chess, Position from, Position to);
    public abstract bool Visit(ChinChessShi chess, Position from, Position to);
    public abstract bool Visit(ChinChessShuai chess, Position from, Position to);

    public bool FaceToFace()
    {
        var shuais = this.GetChesses()
                .Where(c => c.Data.Type == ChessType.帥);

        if(shuais.Count() < 2)
        {
            return true;
        }

        var redShuai = shuais.First(c => c.Data.IsRed == true);
        var blackShuai = shuais.First(c => c.Data.IsRed == false);

        // 王见王
        if (redShuai.Column == blackShuai.Column)
        {
            int fromRow = Math.Min(redShuai.Row, blackShuai.Row);
            int toRow = Math.Max(redShuai.Row, blackShuai.Row);

            var currentRow = fromRow + 1;
            while (currentRow < toRow)
            {
                var currentData = this.GetChessData(currentRow, blackShuai.Column);

                if (!currentData.IsEmpty)
                {
                    return false;
                }

                currentRow++;
            }

            return true;
        }

        return false;
    }

    protected virtual bool TryMoveCore(InnerChinChess chess, Position from, Position to)
    {
        int fromRow = from.Row, fromColumn = from.Column;
        int toRow = to.Row, toColumn = to.Column;

        if (!fromRow.IsInRange(0, 9) || !toRow.IsInRange(0, 9)
            || !fromColumn.IsInRange(0, 8) || !toColumn.IsInRange(0, 8))
        {
            return false;
        }

        return true;
    }

    protected override void DisposeCore()
    {
        this._datas = null;

        base.DisposeCore();
    }
}
