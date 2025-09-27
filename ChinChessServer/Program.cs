#pragma warning disable CS8600 // �� null �����������Ϊ null ��ֵת��Ϊ�� null ���͡�
#pragma warning disable CS8604 // �������Ͳ�������Ϊ null��
#pragma warning disable CS8620 // �����������͵Ŀ�Ϊ null �Բ��죬ʵ�β��������βΡ�
namespace ChinChessServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSignalR();

        var app = builder.Build();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapHub<ChinChessHub>("/ChinChess");

        app.Run();
    }
}