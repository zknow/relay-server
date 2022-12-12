using System.Text;

namespace RelayServer.Core;

public enum Protocol
{
    Ping = 0,
    CreateRoom,
    JoinRoom = 10,
    SyncPlayers,
    RPC,
    RPCWithUID,
}

public class PackHeader
{
    public const string ConstHeader = "qpp";

    public static int ConstHeaderLen => Encoding.ASCII.GetBytes(ConstHeader).Length;

    public const int ConstDataLen = 4; // header後的封包長度，int佔4個Bytes
}