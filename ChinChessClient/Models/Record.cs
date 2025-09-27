namespace ChinChessClient.Models;

record Record(int Id, string Name, string Action, bool IsRed)
{
    public DateTime Time { get; } = DateTime.Now;
}