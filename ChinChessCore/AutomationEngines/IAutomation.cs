using ChinChessCore.Models;

namespace ChinChessCore.AutomationEngines
{
    public interface IAutomation
    {
        MovePath ConvertFrom(string moveInfo);

        string ConvertTo(MovePath movePath);
    }
}