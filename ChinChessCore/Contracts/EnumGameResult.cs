using System.ComponentModel;

namespace ChinChessCore.Contracts
{
    public enum EnumGameResult
    {
        /// <summary>
        /// 战斗中
        /// </summary>
        [Description("战斗中")]
        During,
        /// <summary>
        /// 胜负已分
        /// </summary>
        [Description("胜负已分")]
        VictoryOrDefeat,
        /// <summary>
        /// 平局
        /// </summary>
        [Description("平局")]
        Deuce
    }
}
