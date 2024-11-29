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

using System.Collections.Generic;

namespace MachineVision
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Image _image;
        string[] colorFfileNames = { @"D:\", "sshots", "last_frame.jpg" };
        string[] grayFfileNames = { @"D:\", "sshots", "last_frame_gray.jpg" };
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
            _image = (Bitmap)eventArgs.Frame.Clone();

            try
            {
                MemoryStream ms = new MemoryStream();
                _image.Save(ms, ImageFormat.Bmp);
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
                videoSource.WaitForStop(); // Ожидание завершения потока

                // Сохранение последнего кадра как JPG
                if (_image != null)
                {
                    //преобразование в оттенки серого
                    Bitmap grayImg = ConvertToGrayScale((Bitmap)_image);


                    string filePathColor = Path.Combine(colorFfileNames);
                    string filePathGray = Path.Combine(grayFfileNames);

                    _image.Save(filePathColor, ImageFormat.Jpeg); // Сохранение как JPG
                    grayImg.Save(filePathGray, ImageFormat.Jpeg);
                    MessageBox.Show($"Кадр сохранен: {filePathColor}");
                }
                else
                {
                    MessageBox.Show("Нет кадров для сохранения.");
                }
            }
        }



        Bitmap ConvertToGrayScale(Bitmap originalBMP)
        {
            Bitmap grayBMP = new Bitmap(originalBMP.Width, originalBMP.Height);
            for (int x = 0; x < originalBMP.Width; x++)
            {
                for (int y = 0; y < originalBMP.Height; y++)
                {
                    // Получаем цвет пикселя
                    Color originalColor = originalBMP.GetPixel(x, y);

                    // Вычисляем значение яркости по формуле
                    // Яркость = 0.299*R + 0.587*G + 0.114*B
                    int grayValue = (int)(0.299 * originalColor.R + 0.587 * originalColor.G + 0.114 * originalColor.B);

                    // Создаем новый цвет с серым значением
                    Color grayColor = Color.FromArgb(grayValue, grayValue, grayValue);

                    // Устанавливаем новый серый цвет в соответствующий пиксель
                    grayBMP.SetPixel(x, y, grayColor);
                }
            }
            return grayBMP;
        }
        static System.Drawing.Point? FindImageWithScaling(Bitmap source, Bitmap template)
        {
            for (double scale = 0.5; scale <= 2; scale += 0.1) // Перебор масштабов от 50% до 200%
            {
                using (Bitmap scaledTemplate = new Bitmap(template, new System.Drawing.Size((int)(template.Width * scale), (int)(template.Height * scale))))
                {
                    System.Drawing.Point? foundLocation = FindImage(source, scaledTemplate);
                    if (foundLocation.HasValue)
                    {
                        return foundLocation; // Возвращаем координаты первого найденного совпадения
                    }
                }
            }
            return null; // Если ничего не найдено
        }
        static System.Drawing.Point? FindImage(Bitmap source, Bitmap template)
        {
            for (int x = 0; x <= source.Width - template.Width; x++)
            {
                for (int y = 0; y <= source.Height - template.Height; y++)
                {
                    if (IsMatch(source, template, x, y))
                    {
                        return new System.Drawing.Point(x, y);
                    }
                }
            }
            return null;
        }
        static bool IsMatch(Bitmap source, Bitmap template, int startX, int startY)
        {
            for (int x = 0; x < template.Width; x++)
            {
                for (int y = 0; y < template.Height; y++)
                {
                    Color sourceColor = source.GetPixel(startX + x, startY + y);
                    Color templateColor = template.GetPixel(x, y);
                    if (sourceColor != templateColor)
                    {
                        return false; // Найдено несоответствие
                    }
                }
            }
            return true; // Шаблон найден
        }
        private void FindBlackSquares(Bitmap lastFrame)
        {
            //// Освобождайте неиспользуемые ресурсы (если необходимо)
            //grayBitmap.Dispose();
            //binaryBitmap.Dispose();
        }

        private void Analize_Click(object sender, RoutedEventArgs e)
        {

            // Попробуем найти изображение с разными масштабами
            System.Drawing.Point? foundLocation = FindImageWithScaling(source, template);

            if (foundLocation.HasValue)
            {
                Console.WriteLine($"Изображение найдено в точке: {foundLocation.Value}");
            }
            else
            {
                Console.WriteLine("Изображение не найдено.");
            }
        }
    }
}

