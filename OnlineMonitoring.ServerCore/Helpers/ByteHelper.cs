using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineMonitoring.ServerCore.Helpers
{
    public class ByteHelper
    {
        public static byte[] GetBlockByEndByte(IList<byte> bytes, int startIndex, byte endByte)
        {
            var result = new List<byte>();

            var curentIndex = startIndex;
            while (bytes[curentIndex] != endByte)
            {
                result.Add(bytes[curentIndex]);
                curentIndex++;
            }

            return result.ToArray();
        }

        public static byte[] GetBlockByCount(IEnumerable<byte> bytes, int curentIndex, int count)
        {
            return bytes.Skip(curentIndex).Take(count).ToArray();
        }

        public static string GetBinValue(int inputs, int i)
        {
            var result = Convert.ToString(inputs, 2);

            while (result.Length < i)
            {
                result = result.Insert(0, "0");
            }

            return result;
        }

        public static string GetStringFromBytes(IEnumerable<byte> bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}