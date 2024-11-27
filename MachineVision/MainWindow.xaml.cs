using System;
using System.Windows;
using System.Configuration;
using AForge;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace MachineVision
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Импортируем GDI метод для освобождения HBitmap
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private FilterInfoCollection videoDevices; // Коллекция видеоустройств
        private VideoCaptureDevice videoSource;    // Устройство захвата видео
        public MainWindow()
        {
            InitializeComponent();
            // Получаем список доступных видео устройств
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            GetCameraList();
        }
        private void GetCameraList()
        {
            #region при помощи пакета DirectShowLib
            //DsDevice[] devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            //if (devices.Length == 0)
            //{
            //    MessageBox.Show("Нет доступных видеоприемников.");
            //}
            //else
            //{
            //    for (int i = 0; i < devices.Length; i++)
            //    {
            //        comboBox1.Items.Add(devices[i].Name);
            //    }
            //}
            #endregion


        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoDevices.Count > 0)
            {
                // Создаем видео устройство
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                videoSource.Start(); // Запускаем поток
            }
        }

        private void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Получаем текущий кадр
            var image = (Bitmap)eventArgs.Frame.Clone();

            try
            {
                MemoryStream ms = new MemoryStream();
                image.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();

                bi.Freeze();
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    CameraImage.Source = bi;
                }));

            }
            catch (Exception e)
            {

                throw;
            }

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop(); // Остановка захвата
                videoSource.WaitForStop(); // Ждем завершения потока
            }
        }
    }
}

