using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;

namespace TcpServer.Core.async.block
{
    public class ReceiveTypeSelector
    {
        private ILog log;

        public ReceiveTypeSelector()
        {
            log = LogManager.GetLogger(typeof(ReceiveTypeSelector));
        }

        public void clearFromCRLF(SocketAsyncEventArgs saea, DataHoldingUserToken userToken)
        {
            // допустим что это старт, начало новой команды или новой посылки от блока

            while (saea.BytesTransferred > userToken.bytesDoneThisOp)
            {
                if (saea.Buffer[userToken.bytesDoneThisOp] == 0x0D
                    || saea.Buffer[userToken.bytesDoneThisOp] == 0x0A)
                {
                    userToken.bytesDoneThisOp++;
                }
                else
                {
                    break;
                }
            }
        }

        public void defineTypeData(SocketAsyncEventArgs saea, DataHoldingUserToken userToken)
        {
            var prefix = Encoding.ASCII.GetString(userToken.prefixBytes);

            if (prefix.StartsWith("$$"))
            {
                // это пакет, проверяем его, если он нормальный устанавливаем длину ожидаемого сообщения
                try
                {
                    userToken.messageLength = Convert.ToInt32(prefix.Substring(2), 16) - 4;
                    if (userToken.messageLength > 0)
                    {
                        userToken.dataTypeId = 1;
                    }
                }
                catch
                {
                    log.WarnFormat("Someone sended us a bad packet size prefix={0} his IP={1}", prefix,
                        ((IPEndPoint)saea.AcceptSocket.RemoteEndPoint).Address);
                }
            }
            else if (prefix.StartsWith("Rece"))
            {
                // это ответ на команду
                // например Receive:'015'ok*000000,015,0,195.206.252.247,40181#
                userToken.dataTypeId = 2;
            }
            else if (prefix.StartsWith("AT+C"))
            {
                userToken.dataTypeId = 3;
            }
            else if (prefix.StartsWith("IMEI"))
            {
                userToken.dataTypeId = 4;
            }
            else if (prefix.StartsWith("$V") || userToken.imageReceiving)
            {
                userToken.dataTypeId = 6;
            }
            else
            {
                userToken.dataTypeId = 5;
                // непонятно что прислали
                log.WarnFormat("Someone sended us a bad packet with prefix={0} his IP={1}", prefix,
                    ((IPEndPoint)saea.AcceptSocket.RemoteEndPoint).Address);
            }
        }
    }
}
