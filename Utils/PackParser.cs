using System.Text;

namespace RelayServer.Utils;

public class PackParser
{
    private byte[] buf;
    private int index = 0;

    public PackParser(byte[] rcvBuffer)
    {
        buf = rcvBuffer;
    }

    public PackParser(byte[] rcvBuffer, long offset, long size)
    {
        buf = new byte[size];
        Array.Copy(rcvBuffer, offset, buf, 0, size);
    }

    public bool GetBoolen()
    {
        bool result = BitConverter.ToBoolean(buf, index);
        index += 1;
        return result;
    }

    public byte GetByte()
    {
        byte result = buf[index];
        index += 1;
        return result;
    }

    public string GetString()
    {
        int len = BitConverter.ToInt32(buf, index);
        index += 4;
        string result = Encoding.UTF8.GetString(buf, index, len);
        index += len;
        return result;
    }

    public int GetInt()
    {
        int result = BitConverter.ToInt32(buf, index);
        index += 4;
        return result;
    }

    public long GetLong()
    {
        long result = BitConverter.ToInt64(buf, index);
        index += 8;
        return result;
    }

    public byte[] GetBytes()
    {
        return buf.Skip(index).ToArray();
    }
}