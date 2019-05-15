using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CameraControl.Devices;
using CameraControl.Devices.Canon;
using CameraControl.Devices.Classes;
using Canon.Eos.Framework.Eventing;
using Timer = System.Timers.Timer;

namespace CSNamedPipe
{
    public class ControlDevice
    {
        public CameraDeviceManager DeviceManager { get; set; }
        private Timer _liveViewTimer = new Timer();
        public string FolderForPhotos { get; set; }
        public bool _liveViewStatus { get; set; }
        private int id = 0;

        protected void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("\nControl c push.");
            if (_liveViewStatus == true)
                StopLiveView();
            _liveViewStatus = false;
            Environment.Exit(0);
        }

        public ControlDevice()
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
            DeviceManager = new CameraDeviceManager();
            DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
            DeviceManager.DetectWebcams = false;
            DeviceManager.UseExperimentalDrivers = true;
            DeviceManager.DisableNativeDrivers = false;
            FolderForPhotos = "./tmp/";
        }

        public string ConnectToCamera()
        {
            if (!DeviceManager.ConnectToCamera())
            {
                return "Camera not initialized";
            }
            return "Camera initialized";
        }

        public Socket GlobalSocket;

        public bool Capture(Socket socket)
        {
            bool retry;
            GlobalSocket = socket;
            do
            {
                retry = false;
                try
                {
                    DeviceManager.SelectedCameraDevice.CapturePhoto();
                }
                catch (DeviceException exception)
                {
                    // if device is bussy retry after 100 miliseconds
                    if (exception.ErrorCode == ErrorCodes.MTP_Device_Busy ||
                        exception.ErrorCode == ErrorCodes.ERROR_BUSY)
                    {
                        // !!!!this may cause infinite loop
                        Thread.Sleep(100);
                        retry = true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
            } while (retry);
            return true;
        }

        void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
        {
            PhotoCaptured(eventArgs);
        }

        private void PhotoCaptured(object o)
        {
            PhotoCapturedEventArgs eventArgs = o as PhotoCapturedEventArgs;
            if (eventArgs == null)
                return;
            try
            {
                string fileName = Path.Combine(FolderForPhotos, Path.GetFileName(eventArgs.FileName));
                
                // if file exist try to generate a new filename to prevent file lost. 
                // This useful when camera is set to record in ram the the all file names are same.
                if (File.Exists(fileName))
                    fileName =
                      StaticHelper.GetUniqueFilename(
                        Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_", 0,
                        Path.GetExtension(fileName));

                // check the folder of filename, if not found create it
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }

                eventArgs.CameraDevice.TransferFile(eventArgs.Handle, fileName);
                sendImage(fileName);
                File.Delete(fileName);

                // the IsBusy may used internally, if file transfer is done should set to false  
                eventArgs.CameraDevice.IsBusy = false;
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
                eventArgs.CameraDevice.IsBusy = false;
            }
        }

        void sendImage(string fileName)
        {
            Bitmap bmp = new Bitmap(fileName);
            MemoryStream ms = new MemoryStream();

            bmp.Save(ms, ImageFormat.Jpeg);
            
            bmp.Dispose();
            GlobalSocket.Send(ms);
            ms.Close();
        }

        public void StartLiveView(Socket Socket)
        {
            GlobalSocket = Socket;
            _liveViewTimer.Interval = 1000 / 15;
            _liveViewTimer.Stop();
            _liveViewTimer.Elapsed += _liveViewTimer_Tick;

            bool retry;
            do
            {
                retry = false;
                try
                {
                    DeviceManager.SelectedCameraDevice.StartLiveView();
                }
                catch (DeviceException exception)
                {
                    if (exception.ErrorCode == ErrorCodes.MTP_Device_Busy || exception.ErrorCode == ErrorCodes.ERROR_BUSY)
                    {
                        // this may cause infinite loop
                        Thread.Sleep(100);
                        retry = true;
                    }
                    else
                    {
                        Console.WriteLine("Error occurred :" + exception.Message);
                    }
                }

            } while (retry);
            _liveViewTimer.Start();
            Console.WriteLine("live view started");
        }

        public void _liveViewTimer_Tick(object sender, EventArgs e)
        {
            LiveViewData liveViewData = null;
            try
            {
                liveViewData = DeviceManager.SelectedCameraDevice.GetLiveViewImage();
                Console.WriteLine(liveViewData);
            }
            catch (Exception)
            {
                return;
            }

            if (liveViewData == null || liveViewData.ImageData == null)
            {
                return;
            }
            try
            {
                var image = new MemoryStream(liveViewData.ImageData,
                                                liveViewData.ImageDataPosition,
                                                liveViewData.ImageData.Length -
                                                liveViewData.ImageDataPosition);
                GlobalSocket.Send(image);
            }
            catch (Exception)
            {
            }
        }

        public void StopLiveView()
        {
            bool retry;
            do
            {
                retry = false;
                try
                {
                    _liveViewTimer.Stop();
                    // wait for last get live view image
                    Thread.Sleep(500);
                    DeviceManager.SelectedCameraDevice.StopLiveView();
                    Console.WriteLine("Live view has been stopped");
                }
                catch (DeviceException exception)
                {
                    if (exception.ErrorCode == ErrorCodes.MTP_Device_Busy || exception.ErrorCode == ErrorCodes.ERROR_BUSY)
                    {
                        // this may cause infinite loop
                        Thread.Sleep(100);
                        retry = true;
                    }
                    else
                    {
                        Console.WriteLine("Error occurred :" + exception.Message);
                    }
                }
            } while (retry);
        }

        public string getDeviceName()
        {
            return DeviceManager.SelectedCameraDevice.DeviceName;
        }

        public void connectToPrevDevice()
        {
            DeviceManager.SelectPrevCamera();
        }

        public void connectToNextDevice()
        {
            DeviceManager.SelectNextCamera();
        }

        public ICollection<ICameraDevice> cameraList()
        {
            return DeviceManager.ConnectedDevices;
        }
    }
}
