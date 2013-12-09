using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;
using TcpServer.Core.Properties;

namespace TcpServer.Core.async.block
{
    class ReceiveUPhotoHandler
    {
        private ILog log;
        private ReceiveAllReadedHandler receiveAllReadedHandler = new ReceiveAllReadedHandler();

        public ReceiveUPhotoHandler()
        {
            log = LogManager.GetLogger(typeof(ReceiveUPhotoHandler));
        }

        public int handle(SocketAsyncEventArgs saea, DataHoldingUserToken userToken, out byte[] readyMessage)
        {
            int cnt = 0;
            while (saea.BytesTransferred > (userToken.bytesDoneThisOp + cnt))
            {
                cnt++;
                if (saea.Buffer[userToken.bytesDoneThisOp + cnt - 1] == 0x23)
                {
                    break;
                }
            }

            log.DebugFormat("Photo packet cnt={0}", cnt);

            byte[] message = new byte[cnt + userToken.messageBytesDoneCount];
            if (userToken.messageBytesDoneCount != 0)
            {
                Buffer.BlockCopy(userToken.messageBytes, 0, message, 0, userToken.messageBytesDoneCount);
            }
            Buffer.BlockCopy(saea.Buffer, userToken.bytesDoneThisOp, message, userToken.messageBytesDoneCount, cnt);
            userToken.messageBytes = message;
            userToken.messageBytesDoneCount += cnt;
            userToken.bytesDoneThisOp += cnt;

            if (userToken.messageBytes[userToken.messageBytes.Length - 1] == 0x23)
            {
                readyMessage = new byte[2 + userToken.messageBytes.Length];
                Buffer.BlockCopy(userToken.prefixBytes, 2, readyMessage, 0, 2);
                Buffer.BlockCopy(userToken.messageBytes, 0, readyMessage, 2, userToken.messageBytes.Length);
                userToken.resetReadyMessage();

                string receivedData = Encoding.ASCII.GetString(readyMessage, 0, readyMessage.Length - 1);
                log.DebugFormat("Photo packet={0}", receivedData);

                int indexOfImageBytes = 26;
                int indexOfStartPackage = 0;

                proccessCommonValues(userToken, receivedData, indexOfStartPackage - 2);
                if (userToken.uImageHolder.LastPackageSequence == 1)
                {
                    userToken.uImageHolder.ImageBytes = new byte[userToken.uImageHolder.TotalPackages][];
                }

                string str = receivedData.Substring(indexOfImageBytes);
                userToken.uImageHolder.ImageBytes[userToken.uImageHolder.LastPackageSequence] = StringToByteArray(str);

                if (userToken.uImageHolder.LastPackageSequence == userToken.uImageHolder.TotalPackages)
                {
                    // 1) save image to file
                    saveImageToDisc(userToken);

                    // 2) reset imageHolder
                    userToken.uImageHolder = new UImageHolder();
                }
            }

            readyMessage = null;
            return 0;
        }

        private void proccessCommonValues(DataHoldingUserToken userToken, string receivedData, int startIndex)
        {
            userToken.uImageHolder.IMEI = receivedData.Substring(startIndex + 2, 15);
            userToken.uImageHolder.PictureNumber = Convert.ToInt32(receivedData.Substring(startIndex + 17, 5));
            userToken.uImageHolder.TotalPackages = Convert.ToInt32(receivedData.Substring(startIndex + 22, 3));
            userToken.uImageHolder.LastPackageSequence = Convert.ToInt32(receivedData.Substring(startIndex + 25, 3));
        }

        private void saveImageToDisc(DataHoldingUserToken userToken)
        {
            string imageFilePath = Path.Combine(Settings.Default.ImageSaveDirectory, userToken.uImageHolder.IMEI + ".jpg");
            FileStream fs = new FileStream(imageFilePath, FileMode.Create);
            for (int i = 0; i < userToken.uImageHolder.ImageBytes.Length; i++)
            {
                fs.Write(userToken.uImageHolder.ImageBytes[i], 0, userToken.uImageHolder.ImageBytes[i].Length);
            }
            fs.Close();
        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
