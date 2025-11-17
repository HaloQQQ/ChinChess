using ChinChessClient.Commands;
using ChinChessClient.Contracts;
using ChinChessCore.Models;
using IceTea.Pure.Contracts;

namespace ChinChessClient.AutomationEngines
{
    internal interface IEleEyeEngine : IAutomation, IStarter, IDisposable
    {
        event Action<MovePath> OnBestMoveReceived;

        void InitData(string fen);

        void Move(IReadOnlyList<MoveCommand> moveCommands, int thinkMills);

        void SendCommand(string command);

        void Stop();
    }
}
