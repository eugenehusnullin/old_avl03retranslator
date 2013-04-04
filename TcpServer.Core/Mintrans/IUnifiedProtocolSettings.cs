using System;

namespace TcpServer.Core.Mintrans
{
    public interface IUnifiedProtocolSettings
    {
        string Url { get; }
        string UserName { get; }
        string Password { get; }
        string ImeiListFileName { get; }
        bool Enabled { get; }
        string LoggerName { get; }
    }
}
