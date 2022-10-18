using System.Text;

namespace RelayServer.Models;

public class PackHeader
{
    public const string Header = "qpp";

    public static int HeaderLen => Encoding.ASCII.GetBytes(Header).Length;

    public static int DataLen = 4; // header後的封包長度，int佔4個Bytes
}