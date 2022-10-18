using System.Collections.Concurrent;
using NetCoreServer;
using RelayServer.Models;
using RelayServer.Utils;

public class RoomManager
{
    public ConcurrentDictionary<int, Room> RoomPool = new ConcurrentDictionary<int, Room>();

    public int RoomIdSN = 0;
    public int roomAliveCheckSec = 20;

    public bool TryCreateRoom(long hostUID, int playerNum, out int roomId)
    {
        Interlocked.Increment(ref RoomIdSN);
        var newRoom = new Room
        {
            Lock = new object(),
            HostUID = hostUID,
            PlayerNum = playerNum,
            Players = new List<Player>(),
            CreateTime = DateTime.Now,
        };

        if (RoomPool.TryAdd(RoomIdSN, newRoom))
        {
            roomId = RoomIdSN;
            RoomWaitingHostTask(RoomIdSN);
            return true;
        }
        else
        {
            roomId = -1;
            return false;
        }
    }

    public bool PlayerJoinRoom(long uid, int roomId, TcpSession session)
    {
        if (RoomPool.TryGetValue(roomId, out var room))
        {
            lock (room.Lock)
            {
                bool isHost = (uid == room.HostUID);
                room.Players.Add(new Player()
                {
                    IsHost = isHost,
                    UID = uid,
                    Session = session,
                });

                if (isHost)
                {
                    room.IsHostEnteredRoom = true;
                }
                Console.WriteLine($"Usr:{uid} 加入房間成功，Room{roomId}，IsHost:{isHost}，PlayerNum:{room.Players.Count}");
                return true;
            }
        }
        return false;
    }

    public void SyncHostAndGuest(int roomId, long uid)
    {
        try
        {
            if (!RoomPool.TryGetValue(roomId, out var room))
            {
                return;
            }
            lock (room.Lock)
            {
                Console.WriteLine($"StartSync... RoomPlayer Cnt:{RoomPool[roomId].Players.Count()}");
                var player = room.Players.FirstOrDefault(p => p.UID == uid);
                if (player.IsHost)
                {
                    Console.WriteLine("房主進房");
                    // 同步房主UID給在房間中的Guest
                    var hostUID = player.UID;
                    var existGuests = room.Players.Where(p => !p.IsHost && !p.IsReceivedHost);
                    var pack = RspPackGenerator.CreatSyncHostToGuestsPack(hostUID);
                    Console.WriteLine($"{uid}是房主，同步房主UID給在房間中的Guests，ExistCnt:{existGuests.Count()}");
                    foreach (var guest in existGuests)
                    {
                        guest.IsReceivedHost = true;// 標記已同步
                        guest.Session.SendAsync(pack);
                    }

                    // 同步在房間中的GuestUID給房主
                    var unSyncedGuests = room.Players.Where(p => !p.IsHost && !p.IsSyncedToHost);
                    var guestUIDs = unSyncedGuests.Select(x => x.UID).ToArray();
                    Console.WriteLine($"尚未同步給房主的GuestCount:{unSyncedGuests.Count()}");
                    if (guestUIDs.Count() > 0)
                    {
                        foreach (var guest in unSyncedGuests)
                        {
                            guest.IsSyncedToHost = true; // 標記已同步
                        }
                        Console.WriteLine($"{uid}是房主，同步在房間中的GuestUIDs給房主");
                        pack = RspPackGenerator.CreatSyncGuestsToHostPack(guestUIDs);
                        player.Session.SendAsync(pack);
                    }
                    room.IsHostSyncCompleted = true;
                }
                else
                {
                    //如果房主已在房間，同步現在的房主給Guest & 同步現在的Guest給房主
                    if (room.IsHostEnteredRoom && room.IsHostSyncCompleted)
                    {
                        var hostPlayer = room.Players.FirstOrDefault(p => p.IsHost);

                        if (!player.IsReceivedHost)
                        {
                            Console.WriteLine($"{uid}非房主，同步當前的房主給Guest:{player.UID}");
                            player.IsReceivedHost = true; // 標記已接收房主UID
                            var pack = RspPackGenerator.CreatSyncHostToGuestsPack(hostPlayer.UID);
                            player.Session.SendAsync(pack);
                        }

                        if (!player.IsSyncedToHost)
                        {
                            Console.WriteLine($"{uid}非房主，同步當前的Guest給房主:{player.UID}，房主Session{hostPlayer.Session.Id}");
                            player.IsSyncedToHost = true; // 標記已同步給房主   
                            var pack = RspPackGenerator.CreatSyncGuestsToHostPack(player.UID);
                            hostPlayer.Session.SendAsync(pack);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{uid}非房主，房主不在房間稍後再通知");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public void Brocast(int roomId, byte[] data)
    {
        if (RoomPool.TryGetValue(roomId, out var room))
        {
            var pack = RspPackGenerator.CreatRpcPack(data);
            var sessions = room.Players.Select(x => x.Session);
            foreach (var session in sessions)
            {
                session.SendAsync(pack);
            }
        }
        else
        {
            Console.WriteLine($"Brocast 房間不存在 RoomID:{roomId}");
        }
    }

    public void BrocastToTarget(int roomId, long targetUID, byte[] data)
    {
        if (RoomPool.TryGetValue(roomId, out var room))
        {
            var pack = RspPackGenerator.CreatRpcToTargetPack(data);
            var targetPlayer = room.Players.FirstOrDefault(p => p.UID == targetUID);
            if (targetPlayer != null)
            {
                targetPlayer.Session.SendAsync(pack);
            }
            else
            {
                Console.WriteLine($"Brocast Target:{targetUID} Failed:對象不存在");
            }
        }
        else
        {
            Console.WriteLine($"Brocast Target:{targetUID} Failed:房間不存在");
        }
    }

    private async void RoomWaitingHostTask(int roomId)
    {
        bool roomExist = RoomPool.TryGetValue(roomId, out var room);
        while (roomExist && !room.IsHostEnteredRoom)
        {
            // 超過等待時間，挑掉在房間內的所有人，釋放物件
            if (room.CreateTime.AddSeconds(roomAliveCheckSec) < DateTime.Now)
            {
                Console.WriteLine($"RoomId:{roomId},超過等待時間，踢掉在房間內的所有人，釋放房間物件");
                lock (room.Lock)
                {
                    foreach (var player in room.Players)
                    {
                        player.Session.Disconnect();
                    }
                    RoomPool.TryRemove(roomId, out _);
                }
                break;
            }
            await Task.Delay(1000);
        }
    }

    public void RemovePlayer(int roomId, long uid)
    {
        if (RoomPool.TryGetValue(roomId, out var room))
        {
            lock (room.Lock)
            {
                bool isHost = room.HostUID == uid;
                if (isHost)
                {
                    Console.WriteLine($"RoomId:{roomId},房主離開房間，踢掉在房間內的所有人，釋放房間物件");
                    var players = room.Players.ToArray();
                    foreach (var player in players)
                    {
                        player.Session.Disconnect();
                    }
                    room.Players.Clear();
                    RoomPool.TryRemove(roomId, out _);
                    Console.WriteLine($"RoomPool Cnt:{RoomPool.Count}");
                    return;
                }

                room.Players.RemoveAll(p => p.UID == uid);
                if (room.Players.Count == 0)
                {
                    RoomPool.TryRemove(roomId, out _);
                }
            }
        }
    }
}