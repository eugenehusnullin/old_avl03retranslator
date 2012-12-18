using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TcpServer.CreatePPPPacket
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var packet = "";

            if (ProtocolPPP.Make_PPP_Packet("\xFFFFFFFF\xFFFFFFFF\x04\x0000002A\x01\x57", out packet, 0x0177) > 0)
            {
                byte[] login = Encoding.ASCII.GetBytes(packet);

                textBox1.Text = BitConverter.ToString(login, 0);
            }
        }
    }
}
