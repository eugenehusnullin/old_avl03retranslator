using System.Collections.Generic;
using System.IO;

namespace TcpServer.Core.Mintrans
{
    public class ImeiList
    {
        private MintransSettings settings;
        private HashSet<string> imeiList;

        public ImeiList(MintransSettings settings)
        {
            this.settings = settings;
            this.LoadList();
        }

        private void LoadList()
        {
            this.imeiList = new HashSet<string>();
            if (false == File.Exists(this.settings.ImeiListFileName))
            {
                return;
            }

            using (StreamReader reader = new StreamReader(File.OpenRead(this.settings.ImeiListFileName)))
            {
                while(!reader.EndOfStream)
                {
                    string imei = reader.ReadLine();
                    if(false == string.IsNullOrEmpty(imei))
                    {
                        this.imeiList.Add(imei);
                    }
                }
            }
        }

        public bool IsIncluded(string imei)
        {
            return this.imeiList.Contains(imei);
        }
    }
}