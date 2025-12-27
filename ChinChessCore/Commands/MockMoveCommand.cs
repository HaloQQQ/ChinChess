using ChinChessCore.Models;
using IceTea.Pure.Utils;
using System;

namespace ChinChessCore.Commands
{
    public class MockMoveCommand : ChinChessCommandBase, IChinChessCommand
    {
        public MockMoveCommand(ChinChessModel from, ChinChessModel to)
            : base(from, to)
        {
        }

        public override IDisposable Execute()
        {
            _data = _to.Data;

            _to.SetDataWithoutNotify(_from.Data);
            _from.SetDataWithoutNotify(InnerChinChess.Empty);

            this.Disposer = new DisposeAction(() =>
            {
                _from.SetDataWithoutNotify(_to.Data);
                _to.SetDataWithoutNotify(_data);

                base.DisposeCore();
            });

            return this.Disposer;
        }
    }
}