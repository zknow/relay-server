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