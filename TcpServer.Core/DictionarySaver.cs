using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core
{
    public class DictionarySaver
    {
        public static void Write(Dictionary<int, int> dictionary, string file)
        {
            using (FileStream fs = File.OpenWrite(file))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // Put count.
                writer.Write(dictionary.Count);
                // Write pairs.
                foreach (var pair in dictionary)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }
            }
        }

        public static Dictionary<int, int> Read(string file)
        {
            var result = new Dictionary<int, int>();
            try
            {
                using (FileStream fs = File.OpenRead(file))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    // Get count.
                    int count = reader.ReadInt32();
                    // Read in all pairs.
                    for (int i = 0; i < count; i++)
                    {
                        int key = reader.ReadInt32();
                        int value = reader.ReadInt32();
                        result[key] = value;
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
            return result;
        }
    }
}
