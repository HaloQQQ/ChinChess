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
    private static IDictionary<string, string> _usersNormal = new ConcurrentDictionary<string, string>();
    private static HashSet<string> _waittingUsersNormal = new HashSet<string>();

    private static IDictionary<string, string> _usersJieqi = new ConcurrentDictionary<string, string>();
    private static HashSet<string> _waittingUsersJieqi = new HashSet<string>();

    public async Task Move(string chessInfo)
    {
        var currentUser = Context.ConnectionId;

        if (_usersNormal.TryGetValue(currentUser, out var toUser)
            || _usersJieqi.TryGetValue(currentUser, out toUser))
        {
            var chess = chessInfo.DeserializeObject<ChessInfo>();
            chess.FromUser = currentUser;
            chess.ToUser = toUser;

            await Clients.Clients<IClientProxy>(toUser)
                         .SendAsync("ReceiveMove", chess.SerializeObject());
        }
    }

    public Task<bool> RequestGiveUp()
        => this.Request("RecvGiveUpReq");

    public Task<bool> RequestRevoke()
        => this.Request("RecvRevokeReq");

    public Task<bool> RequestReplay()
        => this.Request("RecvReplayReq");

    private async Task<bool> Request(string clientMethod)
    {
        var currentUser = Context.ConnectionId;

        if (_usersNormal.TryGetValue(currentUser, out var toUser)
            || _usersJieqi.TryGetValue(currentUser, out toUser))
        {
            bool isGiveUp = await Clients.Client(toUser).InvokeAsync<bool>(clientMethod, default);

            return isGiveUp;
        }

        return false;
    }


    private Task SendAsync(string clientId, string method)
        => Clients.Clients<IClientProxy>(clientId)
                        .SendAsync(method);

    private async Task SendAsync(string method)
    {
        var currentUser = Context.ConnectionId;

        if (_usersNormal.TryGetValue(currentUser, out var toUser)
            || _usersJieqi.TryGetValue(currentUser, out toUser))
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

        await this.SendAsync(Context.ConnectionId, "ReceiveConnected");
    }

    public async Task ClientConnected(ChinChessMode chessMode)
    {
        bool isJieQi = chessMode == ChinChessMode.OnlineJieQi;

        IDictionary<string, string> users = isJieQi ? _usersJieqi : _usersNormal;
        HashSet<string> waittingUsers = isJieQi ? _waittingUsersJieqi : _waittingUsersNormal;

        var id = Context.ConnectionId;

        if (waittingUsers.Count > 0)
        {
            string userId = waittingUsers.FirstOrDefault();

            if (!userId.IsNullOrBlank())
            {
                var waittingUser = userId;

                waittingUsers.Remove(waittingUser);

                users.TryAdd(id, waittingUser);
                users.TryAdd(waittingUser, id);

                bool isRed = (DateTime.Now.Second & 1) == 0;

                await AssignRole(waittingUser, isRed);
                await AssignRole(id, !isRed);

                Task AssignRole(string userId, bool isRed)
                    => Clients.Clients(userId)
                              .SendAsync("ReceiveRole", isRed);

                if (chessMode == ChinChessMode.OnlineJieQi)
                {
                    var black = GetRandSeq();
                    var red = GetRandSeq();

                    var seq = black.Concat(red);
                    await PushJieQiSeq(waittingUser, seq);
                    await PushJieQiSeq(id, seq);

                    Task PushJieQiSeq(string userId, IEnumerable<ChessType> seq)
                        => Clients.Clients<IClientProxy>(userId)
                                  .SendAsync("ReceiveJieQi", seq);

                    IList<ChessType> GetRandSeq()
                    {
                        var seqs = new List<ChessType>();

                        var indexs = new List<int>() { 0, 1, 2, 0, 3, 4, 0, 5, 1, 0, 2, 3, 0, 4, 5 };
                        var random = new Random();
                        for (int i = 0; i < 15; i++)
                        {
                            var index = random.Next(0, indexs.Count);

                            switch (indexs[index])
                            {
                                case 0:
                                    seqs.Add(ChessType.兵);
                                    break;
                                case 1:
                                    seqs.Add(ChessType.炮);
                                    break;
                                case 2:
                                    seqs.Add(ChessType.車);
                                    break;
                                case 3:
                                    seqs.Add(ChessType.馬);
                                    break;
                                case 4:
                                    seqs.Add(ChessType.相);
                                    break;
                                case 5:
                                    seqs.Add(ChessType.仕);
                                    break;
                                default:
                                    break;
                            }

                            indexs.RemoveAt(index);
                        }

                        return seqs;
                    }
                }
            }
        }
        else
        {
            waittingUsers.Add(id);
        }

        Console.WriteLine($"{chessMode.GetEnumDescription()}客户端 {id} 已连接");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);

        var exitId = Context.ConnectionId;

        IDictionary<string, string> users = _usersNormal;
        HashSet<string> waittingUsers = _waittingUsersNormal;
        ChinChessMode chessMode = ChinChessMode.Online;

        if (_waittingUsersJieqi.Contains(exitId) || _usersJieqi.TryGetValue(exitId, out var _))
        {
            users = _usersJieqi;
            waittingUsers = _waittingUsersJieqi;
            chessMode = ChinChessMode.OnlineJieQi;
        }

        if (users.TryGetValue(exitId, out var anotherUser))
        {
            users.Remove(exitId);

            if (users.TryGetValue(anotherUser, out var _))
            {
                users.Remove(anotherUser);

                waittingUsers.Add(anotherUser);
                await this.SendAsync(anotherUser, "ReceiveWait");
            }
        }
        else
        {
            waittingUsers.Remove(exitId);
        }

        Console.WriteLine($"{chessMode.GetEnumDescription()}客户端 {exitId} 已断开");
    }
}
