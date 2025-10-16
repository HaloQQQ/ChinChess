using System.ComponentModel;

namespace ChinChessClient.Contracts;

internal enum GameStatus : byte
{
    [Description("未开始")]
    NotInitialized,
    [Description("已就绪")]
    Ready,
    [Description("未就绪")]
    NotReady,
    [Description("已停止")]
    Stoped
}
