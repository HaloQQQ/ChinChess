#pragma warning disable CS8600 // �� null �����������Ϊ null ��ֵת��Ϊ�� null ���͡�
#pragma warning disable CS8604 // �������Ͳ�������Ϊ null��
#pragma warning disable CS8618 // ���˳����캯��ʱ������Ϊ null ���ֶα�������� null ֵ���뿼����� "required" ���η�������Ϊ��Ϊ null��
#pragma warning disable CS8620 // �����������͵Ŀ�Ϊ null �Բ��죬ʵ�β��������βΡ�
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
                "�Ѵ���ͬ�����̣��˳�.." + AppStatics.NewLineChars);

            return;
        }

        var builder = WebApplication.CreateBuilder(args);

        //builder.Services.AddAuthorization();

        //builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSignalR();

        var app = builder.Build();

        //app.UseHttpsRedirection();

        //app.UseAuthorization();

        app.MapHub<ChinChessHub>("/ChinChess");

        app.Run();
    }
}