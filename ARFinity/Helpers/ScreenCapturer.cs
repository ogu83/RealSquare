using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework.Media;
using Microsoft.Phone.Tasks;
using System.Windows.Resources;

namespace ARFinity
{
    public static class ScreenCapturer
    {
        /// <summary>
        /// Captures a UIElement and returns a BitmapImage
        /// </summary>
        /// <param name="e">UserControl to be captured</param>        
        public static void CaptureImage(UserControl e)
        {
            WriteableBitmap bmp = new WriteableBitmap((int)e.ActualWidth, (int)e.ActualHeight);
            bmp.Render(e, null);
            bmp.Invalidate();

            string tempJPEG = "capturedImage.jpg";
            WriteImageToFile(bmp, tempJPEG);
        }

        internal static void WriteImageToFile(WriteableBitmap bmp, string tempJPEG)
        {
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(tempJPEG))
                    myIsolatedStorage.DeleteFile(tempJPEG);

                IsolatedStorageFileStream fileStream = myIsolatedStorage.CreateFile(tempJPEG);
                StreamResourceInfo sri = null;
                Uri uri = new Uri(tempJPEG, UriKind.Relative);
                sri = Application.GetResourceStream(uri);

                Extensions.SaveJpeg(bmp, fileStream, bmp.PixelWidth, bmp.PixelHeight, 0, 85);
                fileStream.Close();
            }

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(tempJPEG, FileMode.Open, FileAccess.Read))
                {
                    MediaLibrary mediaLibrary = new MediaLibrary();
                    Picture pic = mediaLibrary.SavePicture(tempJPEG, fileStream);
                    fileStream.Close();
                }
            }

            PhotoChooserTask photoChooserTask = new PhotoChooserTask();
            photoChooserTask.Show();
        }
    }
}
