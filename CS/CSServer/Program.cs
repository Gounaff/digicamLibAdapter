using CameraControl.Devices;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CSNamedPipe
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
