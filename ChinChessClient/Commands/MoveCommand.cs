using ChinChessClient.Models;

#pragma warning disable IDE0290 // 使用主构造函数
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.Commands;

internal class MoveCommand : ChinChessCommandBase, IChinChessCommand
{
    public int Index { get; }
    public bool IsRed { get; }

    public MoveCommand(int index, bool isRed, ChinChessModel from, ChinChessModel to)
        : base(from, to)
    {
        Index = index;
        IsRed = isRed;
    }

    public override IChinChessCommand Execute()
    {
        CheckDispose();

        _data = _to.Data;

        _to.Data = _from.Data;
        _from.Data = InnerChinChess.Empty;

        return this;
    }

    protected override void DisposeCore()
    {
        _from.Data = _to.Data;

        _to.Data = _data;

        base.DisposeCore();
    }
}
