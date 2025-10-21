using ChinChessClient.Contracts;
using ChinChessCore.Models;

namespace ChinChessClient.Commands;

interface IChinChessCommand : IDestory
{
    Position From { get; }

    Position To { get; }

    IDisposable Execute();

    IDisposable Disposer { get; }
}
