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

namespace VideoDetect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CascadeClassifier cascadeClassifier;
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


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml");
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
        }
        private async void Detect_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPath == null)
            {
                MessageBox.Show("Please select a video file first.");
                return;
            }

            IsProcessing = true;

            VideoCapture capture = new VideoCapture(VideoPath);
            if (!capture.IsOpened)
            {
                MessageBox.Show("Error opening video file.");
                return;
            }

            // using (Window window = new Window("capture"))
            int FPS = Convert.ToInt32(capture.Get(Emgu.CV.CvEnum.CapProp.Fps));
            await Task.Run(() =>
            {
                if (Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}Temp") == false)
                {
                    Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}Temp");
                }
                using (Mat image = new Mat()) // Frame image buffer
                {
                    var frameIndex = 0;

                    bool isProcessing = true;
                    // Loop while we can read an image (aka: image.Empty is not true)
                    do
                    {
                        // Read the next
                        capture.Read(image);

                        // We only want to save every FPS hit since we have 2 images per second -> mod
                        if (frameIndex % (FPS / 2) == 0)
                        {
                            string path = $"{AppDomain.CurrentDomain.BaseDirectory}Temp\\{Guid.NewGuid()}.png";
                            image.Save(path);
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                img.Source = new BitmapImage(new Uri(path));
                            });
                            DetectFaces(image.Clone(), path);
                        }

                        frameIndex++;

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            isProcessing = IsProcessing;
                        });
                    } while (!image.IsEmpty && isProcessing);

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        IsProcessing = false;
                    });
                }
            });
        }
        async Task DetectFaces(Mat image, string imagePath)
        {
            await Task.Run(() =>
            {
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