using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
namespace ChinChessClient.Contracts;

public record ImageRecord
{
    public ImageRecord() { }
    public ImageRecord(string uri)
    {
        Uri = uri;  
    }

    private string _uri;
    public string Uri
    {
        get => _uri;
        set => _uri = value.AssertNotNull(nameof(Uri));
    }

    public string Name => Uri.GetFileName();
}
