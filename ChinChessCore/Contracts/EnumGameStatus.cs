using System.ComponentModel;

namespace ChinChessCore.Contracts
{
    public enum EnumGameStatus : byte
    {
        /// <summary>
        /// 刚打开，不允许操作棋子
        /// </summary>
        [Description("未开始")]
        NotInitialized,
        /// <summary>
        /// 可以开始操作棋子了
        /// </summary>
        [Description("已就绪")]
        Ready,
        /// <summary>
        /// 请求和棋、悔棋、重玩等操作时，等待对方确认，不许操作
        /// </summary>
        [Description("未就绪")]
        NotReady,
        [Description("已停止")]
        Stoped
    }
}
