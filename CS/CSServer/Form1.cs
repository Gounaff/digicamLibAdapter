using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CameraControl.Devices;
using System.Threading;
namespace CSNamedPipe
{
    public partial class Form1 : Form
    {
        private ControlDevice controlDevice { get; set; }
        public Form1()
        {
            InitializeComponent();
            Activated += Form1_Activated;
            controlDevice = new ControlDevice();
            controlDevice.ConnectToCamera();
            Thread socket = new Thread(initSocket);
            socket.Start();
        }
        private void initSocket()
        {
            Socket server = new Socket(11001, controlDevice);
            server.WaitConnection();
            server.StartReceive();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
