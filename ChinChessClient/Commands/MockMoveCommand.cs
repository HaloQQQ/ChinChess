using ChinChessClient.Models;

namespace ChinChessClient.Commands;

internal class MockMoveCommand : ChinChessCommandBase
{
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

        return this;
    }

    protected override void DisposeCore()
    {
        _from.SetDataWithoutNotify(_to.Data);
        _to.SetDataWithoutNotify(_data);

        base.DisposeCore();
    }
}
