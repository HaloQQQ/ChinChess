using ChinChessClient.Models;
using IceTea.Pure.Utils;

#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.Commands;

internal class MoveCommand : ChinChessCommandBase, IChinChessCommand
{
    private Predicate<InnerChinChess> _execute;
    private Predicate<InnerChinChess> _dispose;

    public int Index { get; }
    public bool IsRed { get; }

    public MoveCommand(int index, bool isRed, ChinChessModel from, ChinChessModel to,
        Predicate<InnerChinChess> execute, Predicate<InnerChinChess> dispose)
        : base(from, to)
    {
        Index = index;
        IsRed = isRed;

        _execute = execute.AssertArgumentNotNull(nameof(execute));
        _dispose = dispose.AssertArgumentNotNull(nameof(dispose));
    }

    public override IDisposable Execute()
    {
        _data = _to.Data;

        _to.Data = _from.Data;
        _from.Data = InnerChinChess.Empty;

        _execute(_data);

        this.Disposer = new DisposeAction(() =>
        {
            _from.Data = _to.Data;
            bool hasReturnToOrigin = _to.Data.IsJieQi && _to.Data.OriginPos == _from.Pos;
            if (hasReturnToOrigin)
            {
                _from.Data = _from.OriginData;
            }

            _to.Data = _data;

            _dispose(_data);

            base.DisposeCore();
        });

        return this.Disposer;
    }

    protected override void DisposeCore()
    {
        base.DisposeCore();

        _execute = null;
        _dispose = null;
    }
}
