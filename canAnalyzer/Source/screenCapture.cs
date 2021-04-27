using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;

namespace canAnalyzer
{
    public class ScreenCapture
    {
        static private string getDateTimeBasedFileName (string sExtention)
        {
            DateTime dt = DateTime.Now;
            return string.Format("img_{0}_{1}_{2}_{3}.{4}",           
                dt.Hour.ToString("00"), dt.Minute.ToString("00"), 
                dt.Second.ToString("00"), dt.Millisecond.ToString("000"), 
                sExtention);
        }

        static public string getDateTimeBasedDirPath ()
        {
            DateTime dt = DateTime.Now;

            // base
            string dirPathBase = string.Format("{0}\\{1}\\screenshots",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Application.ProductName);

            return string.Format("{0}\\{1}_{2}_{3}", dirPathBase,
                dt.Year.ToString("0000"), dt.Month.ToString("00"), dt.Day.ToString("00"));
        }


        // save with default dir and name
        static private void saveBitmap (Bitmap bitmap)
        {
            // file name
            string fName = getDateTimeBasedFileName("jpg");

            // path
            string dirPath = getDateTimeBasedDirPath();

            saveBitmap(bitmap, dirPath, fName);
        }

        static private void saveBitmap (Bitmap bitmap, string dirPath, string fName)
        {
            // check existance
            System.IO.Directory.CreateDirectory(dirPath);

            string path = string.Format("{0}\\{1}", dirPath, fName);

            using (MemoryStream memory = new MemoryStream())
            {
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                {
                    bitmap.Save(memory, ImageFormat.Jpeg);
                    byte[] bytes = memory.ToArray();
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        static private Bitmap captureImageScreen()
        {
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(bitmap as Image);
            g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);

            return bitmap;
        }

        static private Bitmap captureImageAppScreen(Form frm)
        {

            Rectangle bounds = frm.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            Graphics g = Graphics.FromImage(bitmap as Image);
            g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);

            return bitmap;
        }

        static public void captureScreen()
        {
            // get
            Bitmap bm = captureImageScreen();
            // save
            saveBitmap(bm);
        }

        static public void captureAppScreen(Form form)
        {
            // get
            Bitmap bm = captureImageAppScreen(form);
            // save
            saveBitmap(bm);
        }
    }
}
