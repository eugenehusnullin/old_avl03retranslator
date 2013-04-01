using System;

namespace TcpServer.Core.Mintrans
{
    public class SoapSinkSettings
    {
        public SoapSinkSettings()
        {
            this.Url = "http://89.175.171.150:6400/gate2";
            this.UserName = "userm";
            this.Password = "passm";
        }

        public string Url { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}