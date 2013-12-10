using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.async.block
{
    public class UImageHolder
    {
        public string IMEI;
        public int PictureNumber;
        public int TotalPackages = 0;
        public int LastPackageSequence = 0;
        public byte[][] ImageBytes = null;
    }
}
