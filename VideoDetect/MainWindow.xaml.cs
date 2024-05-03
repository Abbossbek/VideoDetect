using System.Collections.ObjectModel;
using System.IO;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV;
using VideoDetect.Models;
using System.Text.RegularExpressions;

namespace VideoDetect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<FoundFace> Faces
        {
            get { return (ObservableCollection<FoundFace>)GetValue(FacesProperty); }
            set { SetValue(FacesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Faces.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FacesProperty =
            DependencyProperty.Register("Faces", typeof(ObservableCollection<FoundFace>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<FoundFace>()));



        public bool IsProcessing
        {
            get { return (bool)GetValue(IsProcessingProperty); }
            set { SetValue(IsProcessingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsProcessing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsProcessingProperty =
            DependencyProperty.Register("IsProcessing", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));



        public string VideoPath
        {
            get { return (string)GetValue(VideoPathProperty); }
            set { SetValue(VideoPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoPathProperty =
            DependencyProperty.Register("VideoPath", typeof(string), typeof(MainWindow), new PropertyMetadata(null));



        public int FrameStep
        {
            get { return (int)GetValue(FrameStepProperty); }
            set { SetValue(FrameStepProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImagePerSecond.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FrameStepProperty =
            DependencyProperty.Register("FrameStep", typeof(int), typeof(MainWindow), new PropertyMetadata(1));



        public int FPS
        {
            get { return (int)GetValue(FPSProperty); }
            set { SetValue(FPSProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FPS.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FPSProperty =
            DependencyProperty.Register("FPS", typeof(int), typeof(MainWindow), new PropertyMetadata(0));



        public int ProcessCount
        {
            get { return (int)GetValue(ProcessCountProperty); }
            set { SetValue(ProcessCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ProcessCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProcessCountProperty =
            DependencyProperty.Register("ProcessCount", typeof(int), typeof(MainWindow), new PropertyMetadata(1));





        public string TimerText
        {
            get { return (string)GetValue(TimerTextProperty); }
            set { SetValue(TimerTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TimerText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TimerTextProperty =
            DependencyProperty.Register("TimerText", typeof(string), typeof(MainWindow), new PropertyMetadata(null));

        System.Timers.Timer timer;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OpenVideo_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".mp4";
            dlg.Filter = "Video files (*.mp4)|*.mp4|All files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                VideoPath = dlg.FileName;
                using (var capture = new VideoCapture(VideoPath))
                    FPS = Convert.ToInt32(capture.Get(Emgu.CV.CvEnum.CapProp.Fps));
                FrameStep = FPS;
            }
        }
        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            Faces.Clear();
            img.Source = null;
            await Task.Delay(100);
            if (Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}Temp"))
                try
                {
                    Directory.Delete($"{AppDomain.CurrentDomain.BaseDirectory}Temp", true);
                }
                catch
                {

                }
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            IsProcessing = false;
            timer?.Stop();
        }
        private async void Detect_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPath == null)
            {
                MessageBox.Show("Please select a video file first.");
                return;
            }

            IsProcessing = true;
            int seconds = 0;
            var frameIndex = 0;
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += (s, e) =>
            {
                seconds++;
                App.Current.Dispatcher.Invoke(() =>
                {
                    TimerText = $"Process time: {TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss")}; Video time: {TimeSpan.FromSeconds(frameIndex / FPS).ToString(@"hh\:mm\:ss")}";
                });
            };
            timer.Start();

            List<VideoCapture> captures = new List<VideoCapture>();
            for (int i = 0; i < ProcessCount; i++)
            {
                captures.Add(new VideoCapture(VideoPath));
            }
            if (!captures.First().IsOpened)
            {
                MessageBox.Show("Error opening video file.");
                return;
            }

            if (Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}Temp") == false)
            {
                Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}Temp");
            }
            int step = FrameStep;
            await Task.Run(() =>
            {
                var pr = Parallel.ForEach(captures, async capture =>
                {
                    bool isProcessing = true;
                    // Loop while we can read an image (aka: image.Empty is not true)
                    do
                    {
                        int currentFrame = frameIndex += step;
                        // Read the next
                        capture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, currentFrame);
                        using var image = capture.QueryFrame();
                        if (image == null || image.IsEmpty)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                IsProcessing = false;
                                timer.Stop();
                            });
                            break;
                        }
                        string path = $"{AppDomain.CurrentDomain.BaseDirectory}Temp\\{Guid.NewGuid()}.png";
                        image.Save(path);
                        App.Current.Dispatcher.Invoke(() =>
                    {
                        img.Source = new BitmapImage(new Uri(path));
                    });
                        await DetectFaces(image.Clone(), path);

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            isProcessing = IsProcessing;
                        });

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
        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox.SelectedItem != null && listBox.SelectedItem is FoundFace ff)
            {
                img.Source = new BitmapImage(new Uri(ff.FoundFromImagePath));
            }
        }
    }
}