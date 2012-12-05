using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TcpServer.Core
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
            return GetStringFromBytes(bytes, 0, 0);
        }
        public static string GetStringFromBytes(IEnumerable<byte> bytes, int offset)
        {
            return GetStringFromBytes(bytes, offset, 0);
        }
        public static string GetStringFromBytes(IEnumerable<byte> bytes, int offset, int limit)
        {
            var sb = new StringBuilder();
            var index = 0;
            foreach (var b in bytes)
            {
                index++;
                if (index <= offset) { continue; }
                if (limit > 0 && index >= limit) { break; }
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        public static byte[] GetBytesFromByteString(string bytesString)
        {
            var result = new List<byte>();
            var byteStrinngArray = bytesString.ToCharArray();

            for (int i = 0; i < byteStrinngArray.Length; i = i + 2)
            {
                var firstByte = ConvertCharToNum(byteStrinngArray[i]);
                var lastByte = ConvertCharToNum(byteStrinngArray[i + 1]);

                var byteString = BitConverter.GetBytes(firstByte * 16 + lastByte);
                result.Add(byteString[0]);
            }

            return result.ToArray();
        }

        private static int ConvertCharToNum(char p)
        {
            var result = 0;
            if (p >= 48 && p < 58)
            {
                result = p - 48;
            }
            else if (p >= 65 && p <= 70)
            {
                result = p - 55;
            }

            return result;
        }
    }
}