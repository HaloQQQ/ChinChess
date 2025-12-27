using ChinChessCore.Commands;
using ChinChessCore.Models;
using IceTea.Pure.Contracts;
using System;
using System.Collections.Generic;

namespace ChinChessCore.AutomationEngines
{
    public interface IEleEyeEngine : IAutomation, IStarter, IDisposable
    {
        event Action<MovePath> OnBestMoveReceived;

        void InitData(string fen);

        void Move(IReadOnlyList<MoveCommand> moveCommands, int thinkMills);

        void SendCommand(string command);

        void Stop();
    }
}
