using ChinChessClient.Models;
using IceTea.Pure.Utils;

namespace ChinChessClient.Commands;

internal class MockMoveCommand : ChinChessCommandBase
{
    private bool _isExecuted;

    public MockMoveCommand(ChinChessModel from, ChinChessModel to)
        : base(from, to)
    {
    }

    public override IChinChessCommand Execute()
    {
        CheckDispose();

        _data = _to.Data;

        _to.SetDataWithoutNotify(_from.Data);
        _from.SetDataWithoutNotify(InnerChinChess.Empty);

        _isExecuted = true;

        return this;
    }

    protected override void DisposeCore()
    {
        AppUtils.Assert(_isExecuted, "命令尚未执行");

        _from.SetDataWithoutNotify(_to.Data);
        _to.SetDataWithoutNotify(_data);

        base.DisposeCore();
    }
}
