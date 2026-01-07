using ChinChessCore.Models;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessCore.Visitors
{
    public abstract class VisitorBase : DisposableBase, IVisitor
    {
        private IList<ChinChessModel> _datas;

        public VisitorBase(IList<ChinChessModel> datas)
        {
            _datas = datas.AssertNotNull(nameof(IList<ChinChessModel>));
        }

        public IEnumerable<ChinChessModel> GetChesses() => _datas;

        public ChinChessModel GetChess(Position pos)
        {
            AppUtils.Assert(pos.IsValid, "行列超出范围");

            return _datas[pos.Index];
        }

        public InnerChinChess GetChessData(Position pos) => GetChess(pos).Data;

        public abstract bool Visit(ChinChessJu chess, Position to);
        public abstract bool Visit(ChinChessMa chess, Position to);
        public abstract bool Visit(ChinChessPao chess, Position to);
        public abstract bool Visit(ChinChessBing chess, Position to);
        public abstract bool Visit(ChinChessXiang chess, Position to);
        public abstract bool Visit(ChinChessShi chess, Position to);
        public abstract bool Visit(ChinChessShuai chess, Position to);

        public bool FaceToFace()
        {
            var shuais = this.GetChesses()
                            .Where(c => c.Data.Type == ChessType.帥);

            if (shuais.Count() < 2)
            {
                return false;
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
                    var currentData = this.GetChessData(new Position(currentRow, blackShuai.Column));

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

        protected virtual bool TryMoveCore(InnerChinChess chess, Position to)
            => !chess.IsEmpty && chess.CurPos.IsValid && to.IsValid && chess.CurPos != to;

        protected override void DisposeCore()
        {
            this._datas = null;

            base.DisposeCore();
        }
    }
}