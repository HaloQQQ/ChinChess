using ChinChessCore.Contracts;
using ChinChessCore.Models;
using System;

namespace ChinChessCore.Commands
{
    public interface IChinChessCommand : IDestory
    {
        Position From { get; }

        Position To { get; }

        IDisposable Execute();

        IDisposable Disposer { get; }
    }
}