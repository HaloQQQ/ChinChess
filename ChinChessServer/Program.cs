#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8604 // 引用类型参数可能为 null。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8620 // 由于引用类型的可为 null 性差异，实参不能用于形参。
using IceTea.Pure.Contracts;

namespace ChinChessServer;

public class Program
{
    public static void Main(string[] args)
    {
        new Mutex(true, "ChinChessServer", out bool createdNew);

        if (!createdNew)
        {
            File.AppendAllTextAsync(Path.Combine(AppStatics.DeskTopDir, "ChinChessServer.log"),
                "已存在同名进程，退出.." + AppStatics.NewLineChars);

            return;
        }

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSignalR();

        var app = builder.Build();

        app.MapHub<ChinChessHub>("/ChinChess");

        app.Run();
    }
}