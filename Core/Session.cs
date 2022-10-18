using System.Net.Sockets;
using RelayServer.Utils;

namespace RelayServer.Core;

public class Session : NetCoreServer.TcpSession
{
    private Server server;
    private RoomManager roomMgr;
    private int roomId = -1;
    private long UID = -1;

    public List<byte> tmpPackBuf = new List<byte>();
    public long TickTest = 0;

    public Session(Server server, RoomManager roomMgr) : base(server)
    {
        this.server = server;
        this.roomMgr = roomMgr;
    }

    protected override void OnConnected()
    {
        Console.WriteLine($"Id {Id} connected!");
    }

    protected override void OnDisconnected()
    {
        Console.WriteLine($"Id {Id} disconnected!");
        roomMgr.RemovePlayer(roomId, UID);
    }

    protected override void OnReceived(byte[] rcvBuffer, long offset, long size)
    {
        try
        {
            var buf = new byte[size];
            Array.Copy(rcvBuffer, offset, buf, 0, size);
            tmpPackBuf.AddRange(buf);
            tmpPackBuf = Unpacker.Unpack(tmpPackBuf, SendResponse);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"OnReceived Err:{ex.Message}");
        }
    }

    int rcvCnt = 0;
    private void SendResponse(byte[] rcvBuffer)
    {
        long rcvTick = DateTime.UtcNow.Ticks;
        long elapsedTick = rcvTick - TickTest;
        TickTest = rcvTick;
        TimeSpan ts = new TimeSpan(elapsedTick);
        Console.WriteLine($"elapsedSec : {ts.TotalMilliseconds}\n");

        byte packNo;
        try
        {
            byte[] rspPack;
            var rcvPackParser = new PackParser(rcvBuffer);
            packNo = rcvPackParser.GetByte();
            switch ((Protocol)packNo)
            {
                case Protocol.Ping:
                    rspPack = RspPackGenerator.CreateHeartBeatPack(true);
                    SendAsync(rspPack);
                    break;
                case Protocol.CreateRoom:
                    long packTick = rcvPackParser.GetLong();
                    long hostUID = rcvPackParser.GetLong();
                    int playerNum = rcvPackParser.GetInt();

                    if (roomMgr.TryCreateRoom(hostUID, playerNum, out int createRoomId))
                    {
                        Console.WriteLine($"Create RoomID: {createRoomId}, HostUID:{hostUID}");
                        rspPack = RspPackGenerator.CreateRoomSuccessPack(packTick, createRoomId);
                    }
                    else
                    {
                        rspPack = RspPackGenerator.CreateRoomFailedPack(packTick, "創建房間失敗");
                    }
                    SendAsync(rspPack);
                    break;
                case Protocol.JoinRoom:
                    bool joinSuccess = false;
                    int joinRoomId = rcvPackParser.GetInt();
                    long uid = rcvPackParser.GetLong();
                    Console.WriteLine($"Rcv Join Room:{joinRoomId} UID:{uid}");
                    if (roomMgr.PlayerJoinRoom(uid, joinRoomId, this))
                    {
                        this.roomId = joinRoomId;
                        this.UID = uid;
                        joinSuccess = true;
                        rspPack = RspPackGenerator.CreatJoinRoomPack();
                    }
                    else
                    {
                        rspPack = RspPackGenerator.CreateErrorMsgPack(packNo, "加入房間失敗,房間不存在");
                    }
                    Send(rspPack);

                    // 開始互相同步Host&Guest
                    if (joinSuccess)
                    {
                        // await Task.Delay(100);
                        roomMgr.SyncHostAndGuest(roomId, uid);
                    }
                    break;
                case Protocol.RPC:
                    if (this.roomId != -1)
                    {
                        byte[] rpcData = rcvPackParser.GetBytes();
                        roomMgr.Brocast(roomId, rpcData);
                    }
                    else
                    {
                        Console.WriteLine($"UID:{UID},Call RPC Failed : 尚未加入房間!");
                    }
                    break;
                case Protocol.RPCWithUID:
                    if (this.roomId != -1)
                    {
                        long target = rcvPackParser.GetLong();
                        byte[] rpcData = rcvPackParser.GetBytes();
                        roomMgr.BrocastToTarget(roomId, target, rpcData);
                        rcvCnt++;
                        Console.WriteLine($"RPC Cnt:{rcvCnt}");
                    }
                    else
                    {
                        Console.WriteLine($"UID:{UID},Call RPCWithUID Failed : 尚未加入房間!");
                    }
                    break;
                default:
                    rspPack = RspPackGenerator.CreateErrorMsgPack(packNo, "無法辨識的指令");
                    SendAsync(rspPack);
                    break;
            }
            this.ReceiveAsync();
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"TCP session caught an error with code {error}");
    }
}
