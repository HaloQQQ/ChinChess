using ChinChessCore.Models;

namespace ChinChessClient.Contracts;

internal interface IAutomation
{
    MovePath ConvertFrom(string moveInfo);
    
    string ConvertTo(MovePath movePath);
}
