using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Pure.BaseModels;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
namespace ChinChessClient.Commands;

internal abstract class ChinChessCommandBase : DisposableBase, IChinChessCommand
{
    protected ChinChessModel _from;
    public Position From { get; }

    protected ChinChessModel _to;
    public Position To { get; }

    protected InnerChinChess _data;

    public ChinChessCommandBase(ChinChessModel from, ChinChessModel to)
    {
        _from = from;
        _to = to;

        From = from.Pos;
        To = to.Pos;
    }

    public abstract IChinChessCommand Execute();

    protected override void DisposeCore()
    {
        _from = null;
        _to = null;

        _data = null;

        base.DisposeCore();
    }
}
