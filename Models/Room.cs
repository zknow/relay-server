namespace RelayServer.Models;

public class Room
{
    public object Lock { get; set; }

    public long HostUID { get; set; }

    public int PlayerNum { get; set; }

    public List<Player> Players { get; set; }

    public DateTime CreateTime { get; set; }

    public bool IsHostEnteredRoom { get; set; }

    public bool IsHostSyncCompleted { get; set; }
}