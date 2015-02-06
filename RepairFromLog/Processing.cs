using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepairFromLog
{
    class Processing
    {
        public void start(string repairFilename, string host, int port)
        {
            var fs = File.OpenRead(repairFilename);
            doit(fs, host, port);
        }

        private void doit(FileStream fs, string host, int port)
        {
            using (StreamReader reader = new StreamReader(fs))
            {
                Dictionary<string, Sender> dict = new Dictionary<string, Sender>();

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var imei = line.Substring(4, 15);

                        Sender sender = null;
                        if (!dict.TryGetValue(imei, out sender))
                        {
                            sender = new Sender(imei, host, port);
                            sender.start();
                            dict.Add(imei, sender);
                        }
                        sender.send(line);
                    }
                }

                foreach (var pair in dict)
                {
                    pair.Value.stop();
                }

                foreach (var pair in dict)
                {
                    Console.Out.WriteLine(pair.Value.imei + ": ---- " + pair.Value.count);
                }
            }
        }
    }
}
