using System.ComponentModel;

namespace ChinChessCore.Models
{
    public enum ChinChessMode : byte
    {
        [Description("线上对战")]
        Online,

        [Description("线上揭棋")]
        OnlineJieQi,

        [Description("本地对战")]
        Offline,

        [Description("本地揭棋")]
        OfflineJieQi,

        [Description("本地残局")]
        OfflineCustom,

        [Description("人机对战")]
        OfflineAuto
    }
}
