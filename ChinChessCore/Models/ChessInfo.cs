﻿namespace ChinChessCore.Models
{
    public struct ChessInfo
    {
        public bool FromRed { get; set; }

        public Position From { get; set; }
        public string FromUser { get; set; }

        public Position To { get; set; }
        public string ToUser { get; set; }
    }
}