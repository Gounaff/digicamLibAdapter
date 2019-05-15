using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using CameraControl.Devices;

namespace CSNamedPipe
{
    public class Socket
    {
        private TcpListener Listener;
        ControlDevice controlDevice;
        private TcpClient Client { get; set; }
        private NetworkStream stream;
        public Socket(int port, ControlDevice device)
        {
            controlDevice = device;
            Listener = new TcpListener(IPAddress.Loopback, port);
            Listener.Start();
        }

        public void WaitConnection()
        {
            Client = Listener.AcceptTcpClient();
            stream = Client.GetStream();
        }

        public void StartReceive()
        {
            Thread threadreceive = new Thread(Receive);
            threadreceive.Start();
        }

        public void Send(MemoryStream filestream)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();

            var oui = filestream.Length.ToString();
            byte[] fileByte = filestream.ToArray();
            stream.Write(encoder.GetBytes(oui), 0, fileByte.Length.ToString().Length);
            stream.Flush();
            stream.Write(fileByte, 0, fileByte.Length);
            stream.Flush();
        }

        public void SendMessage(string Ms)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();

            stream.Write(encoder.GetBytes(Ms), 0, Ms.Length);
            stream.Flush();
        }

        public void Receive()
        {
            var buffer = new byte[1024];
            var bytesRead = 0;
            ASCIIEncoding encoder = new ASCIIEncoding();
            string buf;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                buf = encoder.GetString(buffer, 0, bytesRead);
                Console.WriteLine(buf);
                SendMessage(distribute_cmd(buf));
                Array.Clear(buffer, 0, bytesRead);
            }
        }

        public void Stop()
        {
            Listener.Stop();
        }

        private string distribute_cmd(string Ms)
        {
            try
            {
                switch (Ms)
                {
                    case "init_camera":
                        return controlDevice.ConnectToCamera();
                    case "take_picture":
                        return controlDevice.Capture(this).ToString();
                    case "start_live_view":
                        controlDevice.StartLiveView(this);
                        return "ok";
                    case "stop_live_view":
                        controlDevice.StopLiveView();
                        return "ok";
                    case "select_next_camera":
                        controlDevice.connectToNextDevice();
                        return "successfully connect to " + controlDevice.getDeviceName();
                    case "select_prev_camera":
                        controlDevice.connectToPrevDevice();
                        return "successfully connect to " + controlDevice.getDeviceName();
                    case "camera_list":
                        string camList = string.Empty;
                        int index = 1;
                        foreach (ICameraDevice camera in controlDevice.cameraList())
                        {
                            camList += index.ToString() + ": " + camera.DeviceName + "\n";
                            index += 1;
                        }
                        return camList;
                    default:
                        return "Command not found.";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
