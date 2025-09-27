using ChinChessClient.Commands;
using ChinChessClient.Visitors;
using ChinChessCore.Models;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using IceTea.Wpf.Atom.Utils;
using System.Diagnostics;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
namespace ChinChessClient.Models;

[DebuggerDisplay("IsRed={Data.IsRed}, Type={Data.Type}")]
internal class ChinChessModel : NotifyBase, IChineseChess
{
    public int Row { get; }
    public int Column { get; }

    public ChinChessModel(int row, int column)
    {
        this.InitData(row, column);
        this.Row = row;
        this.Column = column;
    }

    private InnerChinChess _data;
    public InnerChinChess Data
    {
        get => _data;
        set => SetProperty(ref _data, value.AssertArgumentNotNull(nameof(Data)));
    }

    public void SetDataWithoutNotify(InnerChinChess newData) => this._data = newData.AssertArgumentNotNull(nameof(Data));

    private bool _isDangerous;
    public bool IsDangerous
    {
        get => _isDangerous;
        set => SetProperty<bool>(ref _isDangerous, value);
    }

    private bool _isReadyToPut;
    public bool IsReadyToPut
    {
        get => _isReadyToPut;
        set => SetProperty(ref _isReadyToPut, value);
    }

    #region IChineseChess
    public bool TryPutTo(IVisitor canEatVisitor, Position to, IList<IChinChessCommand> commandStack, Action<string> publishMsg)
    {
        var target = canEatVisitor.GetChess(to.Row, to.Column);
        //if (this.Data.CanPutTo(canEatVisitor, new Position(this.Row, this.Column), new Position(toRow, toColumn)))
        if (target.IsReadyToPut)
        {
            using (new MockMoveCommand(this, target).Execute())
            {
                var shuai = canEatVisitor.GetChesses().FirstOrDefault(c => c.Data.Type == ChessType.帥 && c.Data.IsRed == target.Data.IsRed);

                if (shuai != null)
                {
                    if (shuai.Data.IsDangerous(canEatVisitor, new Position(shuai.Row, shuai.Column), out _))
                    {
                        publishMsg?.Invoke("在被将军啊，带佬");

                        return false;
                    }
                }
            }

            canEatVisitor.GetChesses().ForEach(c => c.IsReadyToPut = false);
            var command = new MoveCommand(
                                    commandStack.Count + 1,
                                    this.Data.IsRed == true,
                                    this, canEatVisitor.GetChess(to.Row, to.Column)
                                )
                            .Execute();

            WpfAtomUtils.BeginInvoke(() =>
            {
                commandStack.Insert(0, command);
            });

            return true;
        }

        return false;
    }

    public bool TrySelect(IVisitor preMoveVisitor)
        => this.Data.PreMove(preMoveVisitor, new Position(this.Row, this.Column));
    #endregion

    protected override void DisposeCore()
    {
        _data.Dispose();
        _data = null;

        base.DisposeCore();
    }

    private void InitData(int row, int column)
    {
        AppUtils.Assert(row.IsInRange(0, 9), "超出行范围");
        AppUtils.Assert(column.IsInRange(0, 8), "超出列范围");

        bool isRed = row > 4;

        if (row == 0 || row == 9)
        {
            if (column == 0 || column == 8)
            {
                _data = new ChinChessJu(isRed);
                return;
            }
            else if (column == 1 || column == 7)
            {
                _data = new ChinChessMa(isRed);
                return;
            }
            else if (column == 2 || column == 6)
            {
                _data = new ChinChessXiang(isRed);
                return;
            }
            else if (column == 3 || column == 5)
            {
                _data = new ChineseChessShi(isRed);
                return;
            }
            else if (column == 4)
            {
                _data = new ChinChessShuai(isRed);
                return;
            }
        }
        else if (row == 3 || row == 6)
        {
            if (column == 0 || column == 2 || column == 4 || column == 6 || column == 8)
            {
                _data = new ChinChessBing(isRed);
                return;
            }
        }
        else if (row == 2 || row == 7)
        {
            if (column == 1 || column == 7)
            {
                _data = new ChinChessPao(isRed);
                return;
            }
        }

        _data = InnerChinChess.Empty;
    }
}
