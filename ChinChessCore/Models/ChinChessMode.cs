using System.ComponentModel;

namespace ChinChessCore.Models
{
    public enum ChinChessMode : byte
    {
        /// <summary>
        /// 线上对战
        /// </summary>
        [Description("线上对战")]
        Online,

        /// <summary>
        /// 线上揭棋
        /// </summary>
        [Description("线上揭棋")]
        OnlineJieQi,

        /// <summary>
        /// 本地对战
        /// </summary>
        [Description("本地对战")]
        Offline,

        /// <summary>
        /// 本地揭棋
        /// </summary>
        [Description("本地揭棋")]
        OfflineJieQi,

        /// <summary>
        /// 人机对战
        /// </summary>
        [Description("人机对战")]
        OfflineAuto,

        /// <summary>
        /// 本地残局
        /// </summary>
        [Description("本地残局")]
        OfflineCustom,

        /// <summary>
        /// 残局挑战
        /// </summary>
        [Description("残局挑战")]
        OfflineEndGames,

        /// <summary>
        /// 残局解法
        /// </summary>
        [Description("残局解法")]
        OfflineAnswer,
    }
}
