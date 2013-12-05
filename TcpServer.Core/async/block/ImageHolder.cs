using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.async.block
{
    public class ImageHolder
    {
        public string IMEI;
        public int PictureNumber;
        public int TotalPackages = 0;
        public int LastPackageSequence = 0;
        public string Time;
        public string Positioning;
        public byte[][] ImageBytes;

        public bool processing = false;
        public byte[] processingBytes;
    }
}
