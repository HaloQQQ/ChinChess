using ChinChessCore.Models;
using IceTea.Pure.BaseModels;

namespace ChinChessClient.Models;

internal class CustomChess : NotifyBase
{
    public CustomChess(ChessType type, bool isRed, int count)
    {
        Type = type;
        IsRed = isRed;
        Count = count;
    }

    public ChessType Type { get; }

    public bool IsRed { get; }

    public int Count { get; private set; }

    public void Increase()
    {
        Count++;

        RaisePropertyChanged(nameof(Count));
    }

    public void Decrease()
    {
        if (Count > 0)
        {
            Count--;

            RaisePropertyChanged(nameof(Count));
        }
    }
}
