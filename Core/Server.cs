using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using NetCoreServer;
using Serilog;

namespace RelayServer.Core;

public class Server : TcpServer
{
    public static Server Instance;

    public RoomManager RoomMgr;
    private ConcurrentBag<Session> sessionPool;

    public int SessionCount => sessionPool.Count;

    public Server(IPAddress address, int port) : base(address, port)
    {
        this.OptionNoDelay = true;
        this.OptionReuseAddress = true;
    }

    public static void Run()
    {
        Instance = new Server(IPAddress.Any, Config.AppSetting.Port);
        if (!Instance.Start())
        {
            Instance.Dispose();
            Log.Fatal("Start Failed!");
            Environment.Exit(1);
        }
        Log.Information($"Server starting...");
        Instance.RoomMgr = new RoomManager();
        Instance.sessionPool = new ConcurrentBag<Session>();
    }

    protected override TcpSession CreateSession()
    {
        var newSession = new Session(this, RoomMgr);
        sessionPool.Add(newSession);
        return newSession;
    }

    protected override void OnError(SocketError error)
    {
        Log.Error($"TCP server caught an error with code {error}");
    }
}
