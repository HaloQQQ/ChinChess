using ChinChessCore.Models;

namespace ChinChessClient.Commands;

interface IChinChessCommand : IDisposable
{
    public Position From { get; }

    public Position To { get; }

    IChinChessCommand Execute();
}
