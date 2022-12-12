using System.Text;
using RelayServer.Core;
using RelayServer.Models;

namespace RelayServer.Utils;

public class RspPackGenerator
{
    // 封包標頭附加的固定辨識用資料
    public static byte[] AttachHeader(byte[] bytes)
    {
        var buf = new List<byte>();
        buf.AddRange(Encoding.ASCII.GetBytes(PackHeader.ConstHeader));
        buf.AddRange(BitConverter.GetBytes(bytes.Length));
        buf.AddRange(bytes);
        return buf.ToArray();
    }

    // 產生心跳回應封包
    public static byte[] CreateHeartBeatPack(bool isSuccess)
    {
        var buf = new List<byte>() { (byte)Protocol.Ping };
        buf.Add(Convert.ToByte(isSuccess));
        return AttachHeader(buf.ToArray());
    }

    // 產生玩家開房成功回應封包
    public static byte[] CreateRoomSuccessPack(long tick, int roomId)
    {
        var buf = new List<byte>() { (byte)Protocol.CreateRoom };
        buf.Add(Convert.ToByte(true));
        buf.AddRange(BitConverter.GetBytes(tick));
        buf.AddRange(BitConverter.GetBytes(roomId));
        return AttachHeader(buf.ToArray());
    }

    // 產生玩家開房失敗回應封包
    public static byte[] CreateRoomFailedPack(long tick, string errMsg)
    {
        var buf = new List<byte>() { (byte)Protocol.CreateRoom };
        buf.Add(Convert.ToByte(false));
        buf.AddRange(BitConverter.GetBytes(tick));
        var msgBs = Encoding.UTF8.GetBytes(errMsg);
        buf.AddRange(BitConverter.GetBytes(msgBs.Length));
        buf.AddRange(msgBs);
        return AttachHeader(buf.ToArray());
    }

    // 產生玩家加入房間回應封包
    public static byte[] CreatJoinRoomPack()
    {
        var buf = new List<byte>() { (byte)Protocol.JoinRoom };
        buf.Add(Convert.ToByte(true));
        return AttachHeader(buf.ToArray());
    }

    // 同步Host給Guest回應封包
    public static byte[] CreatSyncHostToGuestsPack(long hostUID)
    {
        var buf = new List<byte>() { (byte)Protocol.SyncPlayers };
        buf.Add((byte)SyncPlayersType.SyncHostToGuest);
        buf.AddRange(BitConverter.GetBytes(hostUID));
        return AttachHeader(buf.ToArray());
    }

    // 同步Guests給Host回應封包
    public static byte[] CreatSyncGuestsToHostPack(params long[] uids)
    {
        var buf = new List<byte>() { (byte)Protocol.SyncPlayers };
        buf.Add((byte)SyncPlayersType.SyncGuestToHost);
        buf.AddRange(BitConverter.GetBytes(uids.Length));
        foreach (var uid in uids)
        {
            buf.AddRange(BitConverter.GetBytes(uid));
        }
        return AttachHeader(buf.ToArray());
    }

    public static byte[] CreatRpcPack(byte[] data)
    {
        var buf = new List<byte>() { (byte)Protocol.RPC };
        buf.AddRange(data);
        return AttachHeader(buf.ToArray());
    }

    public static byte[] CreatRpcToTargetPack(byte[] data)
    {
        var buf = new List<byte>() { (byte)Protocol.RPCWithUID };
        buf.AddRange(data);
        return AttachHeader(buf.ToArray());
    }

    // 產生發生錯誤回應封包
    public static byte[] CreateErrorMsgPack(byte method, string msg)
    {
        var buf = new List<byte>() { method };
        buf.Add(Convert.ToByte(false));
        var msgBytes = Encoding.Unicode.GetBytes(msg);
        buf.AddRange(BitConverter.GetBytes(msgBytes.Length));
        buf.AddRange(msgBytes);
        return AttachHeader(buf.ToArray());
    }
}