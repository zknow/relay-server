using NetCoreServer;

namespace RelayServer.Models;

public class Player
{
    public TcpSession? Session { get; set; }

    public long UID { get; set; }

    public bool IsHost { get; set; }

    public bool IsSyncedToHost { get; set; } // Guest已同步給Host Flag

    public bool IsReceivedHost { get; set; } // Guest已接收Host Flag
}