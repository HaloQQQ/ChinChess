using ChinChessCore.Models;
using IceTea.Atom.Extensions;
using IceTea.Pure.Extensions;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8604 // 引用类型参数可能为 null。
namespace ChinChessServer;

public class ChinChessHub : Hub
{
    private static IDictionary<string, string> _users = new ConcurrentDictionary<string, string>();

    private static HashSet<string> _waittingUsers = new HashSet<string>();

    public async Task Move(string chessInfo)
    {
        var currentUser = Context.ConnectionId;

        if (_users.TryGetValue(currentUser, out var toUser))
        {
            var chess = chessInfo.DeserializeObject<ChessInfo>();
            chess.FromUser = currentUser;
            chess.ToUser = toUser;

            await Clients.Clients<IClientProxy>(toUser)
                         .SendAsync("ReceiveMove", chess.SerializeObject());
        }
    }

    private Task SendAsync(string clientId, string method)
        => Clients.Clients<IClientProxy>(clientId)
                        .SendAsync(method);

    private async Task SendAsync(string method)
    {
        var currentUser = Context.ConnectionId;

        if (_users.TryGetValue(currentUser, out var toUser))
        {
            await Clients.Clients<IClientProxy>(toUser)
                         .SendAsync(method);
        }
    }

    public Task Revoke()
        => SendAsync("ReceiveRevoke");

    public Task StartPause()
        => SendAsync("ReceiveStartPause");

    public Task RePlay()
        => SendAsync("ReceiveRePlay");

    // 连接生命周期管理
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        var id = Context.ConnectionId;

        if (_waittingUsers.Count > 0)
        {
            string userId = _waittingUsers.FirstOrDefault();

            if (!userId.IsNullOrBlank())
            {
                var waittingUser = userId;

                _waittingUsers.Remove(waittingUser);

                _users.TryAdd(id, waittingUser);
                _users.TryAdd(waittingUser, id);

                bool isRed = (DateTime.Now.Second & 1) == 0;

                await AssignRole(waittingUser, isRed);
                await AssignRole(id, !isRed);

                Task AssignRole(string userId, bool isRed)
                    => Clients.Clients<IClientProxy>(userId)
                              .SendAsync("SetRole", isRed);
            }
        }
        else
        {
            _waittingUsers.Add(id);
        }

        Console.WriteLine($"客户端 {id} 已连接");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);

        var exitId = Context.ConnectionId;

        if (_users.TryGetValue(exitId, out var anotherUser))
        {
            _users.Remove(exitId);

            if (_users.TryGetValue(anotherUser, out var exit))
            {
                _users.Remove(anotherUser);

                _waittingUsers.Add(anotherUser);
                await this.SendAsync(anotherUser, "ReceiveWait");
            }
        }
        else
        {
            _waittingUsers.Remove(exitId);
        }

        Console.WriteLine($"客户端 {exitId} 已断开");
    }
}
