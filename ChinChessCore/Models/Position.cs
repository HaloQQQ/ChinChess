namespace ChinChessCore.Models
{
#pragma warning disable CS0660 // 类型定义运算符 == 或运算符 !=，但不重写 Object.Equals(object o)
#pragma warning disable CS0661 // 类型定义运算符 == 或运算符 !=，但不重写 Object.GetHashCode()
    public struct Position
    {
        public Position(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }

        /// <summary>
        /// 根据坐标计算数据索引
        /// </summary>
        public int Index => Row * 9 + Column;

        public static bool operator ==(Position left, Position right)
            => left.Row == right.Row && left.Column == right.Column;

        public static bool operator !=(Position left, Position right)
            => !(left == right);

        public override string ToString() => $"({this.Row},{this.Column})";
    }
}