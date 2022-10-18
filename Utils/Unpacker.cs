using System.Text;
using RelayServer.Models;

namespace RelayServer.Utils;

public class Unpacker
{
    public static List<byte> Unpack(List<byte> packList, Action<byte[]> legitPackCallback = null)
    {
        byte[] buf = packList.ToArray();
        int len = buf.Length;

        int i = 0;
        for (i = 0; i < len; i++)
        {
            // Check封包長度是否大於等於HeaderLen長度，否=>不處理，等待下一包
            if (len < i + PackHeader.HeaderLen + PackHeader.DataLen)
                break;

            string bufHeader = Encoding.ASCII.GetString(buf[i..(i + PackHeader.HeaderLen)]);
            if (bufHeader == PackHeader.Header)
            {
                byte[] msgLenBytes = buf[(i + PackHeader.HeaderLen)..(i + PackHeader.HeaderLen + PackHeader.DataLen)];
                int msgLen = BitConverter.ToInt32(msgLenBytes);

                // Check未檢查封包長度是否超過原始風包長度，是=>不處理，等待下一包封包
                int unhandledPackLen = PackHeader.HeaderLen + PackHeader.DataLen + msgLen;
                if (len < i + unhandledPackLen)
                    break;

                int unHandledPackIndex = (i + PackHeader.HeaderLen + PackHeader.DataLen);
                byte[] legitPack = buf[(unHandledPackIndex)..(unHandledPackIndex + msgLen)];
                legitPackCallback?.Invoke(legitPack);

                i += unhandledPackLen - 1;
            }
        }

        // i == len：代表整個封包已成功解析且合法，不需要把剩餘的byte回傳
        // buf[^i]：回傳位置i以後的bytes，因為之前的bytes皆解析過，已經不重要了，只需把未檢查的內容回傳
        return (i == len) ? new List<byte>() : new List<byte>(buf[^i]);
    }
}
