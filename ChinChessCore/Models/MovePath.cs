namespace ChinChessCore.Models
{
    public struct MovePath
    {
        public MovePath(Position from, Position to)
        {
            From = from;
            To = to;
        }

        public Position From { get; }
        public Position To { get; }
    }
}
