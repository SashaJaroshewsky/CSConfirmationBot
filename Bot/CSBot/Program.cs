using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Windows.Forms;
using WindowsInput;
using static System.Windows.Forms.LinkLabel;

namespace CSBot
{
    internal class Program
    {
        private static readonly InputSimulator _sim = new InputSimulator();

        private static readonly string _confirmButtonImagePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "ConfirmButton.png");
        //private static readonly Rectangle _confirmButtonSearchRegion = new Rectangle(857, 42, 210, 70);

        static async Task Main(string[] args)
        {
            await Task.WhenAny(Task.Run(StartMonitoringLoop));
        }

        private async static Task StartMonitoringLoop()
        {
            while (true)
            {
                await StartMonitoring();
            }
        }


        private static async Task StartMonitoring()
        {
            if (!File.Exists(_confirmButtonImagePath))
            {
                throw new FileNotFoundException($"Файл {_confirmButtonImagePath} не знайдено.");
            }

            using (var confirmButtonTemplate = Cv2.ImRead(_confirmButtonImagePath, ImreadModes.Grayscale))
            {
                //var screen = CaptureRegion(_confirmButtonSearchRegion).CvtColor(ColorConversionCodes.BGR2GRAY);
                var screen = CaptureFullScreen().CvtColor(ColorConversionCodes.BGR2GRAY);

                using (var result = new Mat())
                {
                    Cv2.MatchTemplate(screen, confirmButtonTemplate, result, TemplateMatchModes.CCoeffNormed);
                    Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point position);

                    if (maxVal >= 0.9)
                    { 
                        Console.WriteLine($"Кнопка знайдена! x={position.X + (confirmButtonTemplate.Width/2)}, y={position.Y + confirmButtonTemplate.Height / 2}");

                        OpenCvSharp.Point centerPoint = new OpenCvSharp.Point(position.X + confirmButtonTemplate.Width / 2, position.Y + confirmButtonTemplate.Height / 2);

                        //Cv2.Rectangle(screen, position, new OpenCvSharp.Point(position.X + confirmButtonTemplate.Width, position.Y + confirmButtonTemplate.Height), new Scalar(0, 0, 255), 2);
                        //Cv2.ImWrite("result_screen.png", screen);

                        ClickConfirmationButton(centerPoint);

                        await Task.Delay(2000);
                        return;
                    }
                    Console.WriteLine($"Кнопка не знайдена");
                }
            }
            await Task.Delay(1000);
        }

        private static void ClickConfirmationButton(OpenCvSharp.Point point)
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            double normalizedX = (point.X * 65535) / screenWidth;
            double normalizedY = (point.Y * 65535) / screenHeight;


            _sim.Mouse.MoveMouseTo(normalizedX, normalizedY);
            Task.Delay(1000);
            _sim.Mouse.LeftButtonClick();

        }

        static Mat CaptureFullScreen()
        {
            using (var bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                }
                return BitmapConverter.ToMat(bmp);
            }
        }

        static Mat CaptureRegion(Rectangle region)
        {
            using (var bmp = new Bitmap(region.Width, region.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(region.X, region.Y, 0, 0, bmp.Size);
                }
                return BitmapConverter.ToMat(bmp);
            }
        }


    }
}
