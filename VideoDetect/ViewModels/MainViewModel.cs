using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

using Emgu.CV;

using VideoDetect.Models;

namespace VideoDetect.ViewModels
{
    public class MainViewModel : BindableBase
    {

        private bool isProcessing;
        public bool IsProcessing
        {
            get { return isProcessing; }
            set { SetProperty(ref isProcessing, value); }
        }
        private string videoPath;
        public string VideoPath
        {
            get { return videoPath; }
            set { SetProperty(ref videoPath, value); }
        }
        private ObservableCollection<FoundFace> faces = new();
        public ObservableCollection<FoundFace> Faces
        {
            get { return faces; }
            set { SetProperty(ref faces, value); }
        }
        private BitmapImage imageSource;
        public BitmapImage ImageSource
        {
            get { return imageSource; }
            set { SetProperty(ref imageSource, value); }
        }
        public void Stop()
        {
            IsProcessing = false;
        }
        public void OpenVideo()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".mp4";
            dlg.Filter = "Video files (*.mp4)|*.mp4|All files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                VideoPath = dlg.FileName;
            }
        }
        public async void Clear()
        {
            Faces.Clear();
            OnPropertyChanged(nameof(Faces));
            await Task.Delay(100);
            if (System.IO.Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}Temp"))
                try
                {
                    System.IO.Directory.Delete($"{AppDomain.CurrentDomain.BaseDirectory}Temp", true);
                }
                catch
                {

                }
        }
        public async Task Detect()
        {
            if (VideoPath == null)
            {
                MessageBox.Show("Please select a video file first.");
                return;
            }

            IsProcessing = true;

            List<VideoCapture> captures = new List<VideoCapture>();
            for (int i = 0; i < 1; i++)
            {
                captures.Add(new VideoCapture(VideoPath));
            }
            if (!captures.First().IsOpened)
            {
                MessageBox.Show("Error opening video file.");
                return;
            }

            int FPS = Convert.ToInt32(captures.First().Get(Emgu.CV.CvEnum.CapProp.Fps));
            var frameIndex = 0;
            if (Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}Temp") == false)
            {
                Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}Temp");
            }
            await Task.Run(() =>
            {
                Parallel.ForEach(captures, async capture =>
                {
                    // Loop while we can read an image (aka: image.Empty is not true)
                    do
                    {
                        int currentFrame = frameIndex += (FPS / 2);
                        // Read the next
                        capture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, currentFrame);
                        using var image = capture.QueryFrame();
                        if (image == null || image.IsEmpty)
                        {
                            IsProcessing = false;
                            break;
                        }
                        string path = $"{AppDomain.CurrentDomain.BaseDirectory}Temp\\{Guid.NewGuid()}.png";
                        image.Save(path);
                        //    App.Current.Dispatcher.Invoke(() =>
                        //{
                        ImageSource = new BitmapImage(new Uri(path));
                        //});
                        await DetectFaces(image.Clone(), path);

                    } while (isProcessing);
                });
            });
        }
        async Task DetectFaces(Mat image, string imagePath)
        {
            await Task.Run(() =>
            {
                using var cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml");
                var rects = cascadeClassifier.DetectMultiScale(image, 1.08, 30, new System.Drawing.Size(30, 30), new System.Drawing.Size(300, 300));

                foreach (var rect in rects)
                {
                    // get face image
                    var face = new Mat(image, rect);
                    string facePath = $"{AppDomain.CurrentDomain.BaseDirectory}Temp\\{Guid.NewGuid()}.png";
                    face.Save(facePath);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Faces.Add(new FoundFace { ImagePath = facePath, FoundFromImagePath = imagePath });
                    });
                }
                image.Dispose();
            });
        }
        public void FaceSelected(FoundFace face)
        {
            ImageSource = new BitmapImage(new Uri(face.ImagePath));
        }
    }
}
