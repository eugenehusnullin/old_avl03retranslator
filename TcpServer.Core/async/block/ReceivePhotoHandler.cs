using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;
using TcpServer.Core.Properties;

namespace TcpServer.Core.async.block
{
    class ReceivePhotoHandler
    {
        private ILog log;
        private ReceiveAllReadedHandler receiveAllReadedHandler = new ReceiveAllReadedHandler();

        public ReceivePhotoHandler()
        {
            log = LogManager.GetLogger(typeof(ReceivePhotoHandler));
        }

        public int handle(SocketAsyncEventArgs saea, DataHoldingUserToken userToken, out byte[] readyMessage)
        {
            receiveAllReadedHandler.handle(saea, userToken, out readyMessage);
            if (userToken.imageHolder.processing)
            {
                userToken.imageHolder.processing = false;
                byte[] arr = readyMessage;
                readyMessage = new byte[userToken.imageHolder.processingBytes.Length + arr.Length];
                Buffer.BlockCopy(userToken.imageHolder.processingBytes, 0, readyMessage, 0, userToken.imageHolder.processingBytes.Length);
                Buffer.BlockCopy(arr, 0, readyMessage, userToken.imageHolder.processingBytes.Length, arr.Length);
            }
            string receivedData = Encoding.ASCII.GetString(readyMessage);
            log.DebugFormat("Photo packet={0}", receivedData);

            int indexOfImageBytes = 28;
            if (userToken.imageHolder.LastPackageSequence == 0)
            {
                indexOfImageBytes = 59;
            }

            int indexOfStartPackage = 0;
            while (readyMessage.Length > indexOfImageBytes + 3)
            {
                int indexOfDiez = receivedData.IndexOf("#\r\n", indexOfImageBytes);
                if (indexOfDiez != -1)
                {
                    if (userToken.imageHolder.LastPackageSequence == 0)
                    {
                        processFirstPackage(userToken, receivedData, readyMessage);
                    }
                    else
                    {
                        proccessCommonValues(userToken, receivedData, indexOfStartPackage);
                    }

                    byte[] arr2 = new byte[indexOfDiez - indexOfImageBytes];
                    Buffer.BlockCopy(readyMessage, indexOfImageBytes, arr2, 0, arr2.Length);
                    userToken.imageHolder.ImageBytes[userToken.imageHolder.LastPackageSequence] = arr2;

                    indexOfStartPackage = indexOfDiez + 3;
                    indexOfImageBytes = indexOfStartPackage + 28;
                }
                else
                {
                    break;
                }
            }


            if (userToken.imageHolder.LastPackageSequence != userToken.imageHolder.TotalPackages)
            {
                needLoadMore(userToken, readyMessage, indexOfStartPackage);
            }
            else
            {
                // 1) save image to file
                saveImageToDisc(userToken);

                // 2) reset imageHolder
                userToken.imageHolder = new ImageHolder();
                userToken.imageReceiving = false;
            }

            readyMessage = null;
            return 0;
        }

        private void processFirstPackage(DataHoldingUserToken userToken, string receivedData, byte[] readyMessage)
        {
            proccessCommonValues(userToken, receivedData, 0);
            userToken.imageHolder.Time = receivedData.Substring(28, 12);
            userToken.imageHolder.Positioning = receivedData.Substring(40, 19);
            userToken.imageHolder.ImageBytes = new byte[userToken.imageHolder.TotalPackages][];
        }

        private void proccessCommonValues(DataHoldingUserToken userToken, string receivedData, int startIndex)
        {
            userToken.imageHolder.IMEI = receivedData.Substring(startIndex + 2, 15);
            userToken.imageHolder.PictureNumber = Convert.ToInt32(receivedData.Substring(startIndex + 17, 5));
            userToken.imageHolder.TotalPackages = Convert.ToInt32(receivedData.Substring(startIndex + 22, 3));
            userToken.imageHolder.LastPackageSequence = Convert.ToInt32(receivedData.Substring(startIndex + 25, 3));
        }

        private void needLoadMore(DataHoldingUserToken userToken, byte[] messageToSave, int startIndex)
        {
            userToken.imageHolder.processingBytes = new byte[messageToSave.Length - startIndex];
            Buffer.BlockCopy(messageToSave, startIndex, userToken.imageHolder.processingBytes, 0, userToken.imageHolder.processingBytes.Length);
            userToken.imageHolder.processing = true;
            userToken.imageReceiving = true;
        }

        private void saveImageToDisc(DataHoldingUserToken userToken)
        {
            string imageFilePath = Path.Combine(Settings.Default.ImageSaveDirectory, userToken.imageHolder.IMEI
                + "-" + userToken.imageHolder.Time + "-" + userToken.imageHolder.Positioning + ".jpg");
            FileStream fs = new FileStream(imageFilePath, FileMode.Create);
            for (int i = 0; i < userToken.imageHolder.ImageBytes.Length; i++)
            {
                fs.Write(userToken.imageHolder.ImageBytes[i], 0, userToken.imageHolder.ImageBytes[i].Length);
            }
            fs.Close();
        }
    }
}
