using System.ComponentModel;

namespace ChinChessCore.Models
{
    public enum ChinChessMode : byte
    {
        [Description("线上")]
        /// <summary>
        /// 线上
        /// </summary>
        Online,

        [Description("本地")]
        /// <summary>
        /// 本地
        /// </summary>
        Offline,

        [Description("线上揭棋")]
        /// <summary>
        /// 线上揭棋
        /// </summary>
        OnlineJieQi,

        [Description("本地揭棋")]
        /// <summary>
        /// 本地揭棋
        /// </summary>
        OfflineJieQi,

        [Description("本地残局")]
        /// <summary>
        /// 本地残局
        /// </summary>
        OfflineCustom
    }
}
