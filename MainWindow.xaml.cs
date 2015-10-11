using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using MusicTimeCore;
using Timer = System.Timers.Timer;

namespace MusicTimeGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            manager = new AudioManager();
            _history = new List<KnownSongInfo>();

            Timer t = new Timer(1000);
            t.Elapsed += Tick;
            t.Start();
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            if (!_isSongPlaying) return;
            if (manager.Finished)
            {
                _isSongPlaying = false;
                Dispatcher.BeginInvoke((Action) delegate
                {
                    Title = "MusicTime";
                });
                return;
            }
            TotalTimeLabel.Dispatcher.BeginInvoke((Action) delegate
            {
                Title = "MusicTime | " + manager.StatusString;
                ProgresSlider.Maximum = (int)manager.CurrentMedia.duration;
                TotalTimeLabel.Content = manager.CurrentMedia.durationString;
                CurrentTimeLabel.Content = manager.CurrentTimeString;
                double percentage = (manager.CurrentTime / manager.CurrentMedia.duration);
                ProgresSlider.Value = (int) (percentage*ProgresSlider.Maximum);
                if (_currentSong == -1 || _currentSong == _history.Count - 1)
                {
                    NextButton.IsEnabled = false;
                }
                else
                {
                    NextButton.IsEnabled = true;
                }

                if ((_currentSong == -1 || _currentSong == 0) && percentage < 0.05d)
                {
                    PrevButton.IsEnabled = false;
                }
                else
                {
                    PrevButton.IsEnabled = true;
                }
            });
        }

        private bool _isSongPlaying;
        private bool _isPaused;
        private List<KnownSongInfo> _history;
        private int _currentSong = -1;

        private bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                //PlayPauseButton.Content = value ? "Play" : "Pause";
                PlayPauseButton.Content = value ? ">" : "||";
            }
        }

        private AudioManager manager;
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string query = textBox.Text;
            if (string.IsNullOrWhiteSpace(query)) return;
            Thread searchThread = new Thread((ThreadStart) delegate
            {
                SearchFeedbackLabel.Dispatcher.BeginInvoke((Action) delegate
                {
                    SearchFeedbackLabel.Content = "Searching...";
                    textBox.IsEnabled = false;
                    searchButton.IsEnabled = false;
                });
                IEnumerable<Song> result;
                try
                {
                    result = SourceFetch.Search(query);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Web Error!");
                    return;
                }

                ObservableCollection<KnownSongInfo> songs = new ObservableCollection<KnownSongInfo>();
                foreach (Song song in result)
                {
                    var s = new KnownSongInfo() {Artist = song.Artist, Name = song.Name};
                    s.SetSong(song);
                    songs.Add(s);
                }
                SearchResultsGrid.Dispatcher.BeginInvoke((Action)delegate
                {
                    SearchResultsGrid.ItemsSource = songs;
                });
                SearchFeedbackLabel.Dispatcher.BeginInvoke((Action)delegate
                {
                    SearchFeedbackLabel.Content = "";
                    textBox.IsEnabled = true;
                    searchButton.IsEnabled = true;
                });
            });
            searchThread.Start();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(manager == null) return;
            manager.Volume = (int)VolumeSlider.Value;
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsPaused)
            {
                manager.Resume();
                IsPaused = false;
            }
            else
            {
                manager.Pause();
                IsPaused = true;
            }
        }

        private void SearchResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid) sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if(row == null) return;
            var item = SearchResultsGrid.Items[row.GetIndex()] as KnownSongInfo;
            if(item == null) return;
            PlaySong(item);
            if (_currentSong != -1 && _currentSong != _history.Count - 1)
            {
                _history.RemoveRange(_currentSong, _history.Count - _currentSong);
            }
            _history.Add(item);
            _currentSong = _history.Count - 1;
        }

        private void PlaySong(KnownSongInfo song)
        {
            manager.Stop();
            songNameLabel.Content = song.Name;
            songArtistLabel.Content = song.Artist;
            _isSongPlaying = true;
            IsPaused = false;
            manager.Play(song.GetSong().Uri);
            SetSongPicture(song);
        }


        private void SetSongPicture(KnownSongInfo info)
        {
            Thread pic = new Thread((ThreadStart) delegate
            {
                string uri;
                try
                {
                    uri = info.GetSong().AdditionalInfo.CoverUri64;
                }
                catch (WebException ex)
                {
                    MessageBox.Show(ex.Message, "Web Error!");
                    return;
                }
                catch (NullReferenceException)
                {
                    songPicture.Dispatcher.BeginInvoke((Action) delegate
                    {
                        songPicture.Source = new BitmapImage(new Uri("DefaultAlbum.png", UriKind.Relative));
                    });
                    return;
                }
                SetPicture(uri);
            });
            pic.Start();
        }

        private void SetPicture(string uri)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.Invoke(new Action<string>(SetPicture), uri);
            else
            {
                BitmapImage bitm = new BitmapImage(new Uri(uri));
                songPicture.Source = bitm;
            }
        }

        private void ProgresSlider_ValueChanged(object sender, DragCompletedEventArgs e)
        {
            manager.CurrentTime = ProgresSlider.Value;
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            double percentage = (manager.CurrentTime/manager.CurrentMedia.duration);
            if (percentage > 0.05d)
            {
                manager.CurrentTime = 0;
                return;
            }
            _currentSong--;
            PlaySong(_history[_currentSong]);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            _currentSong++;
            PlaySong(_history[_currentSong]);
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_Click(new object(), new RoutedEventArgs());
            }
        }
    }


    public class KnownSongInfo
    {
        public string Artist { get; set; }
        public string Name { get; set; }

        private Song _internalSong;

        public void SetSong(Song song)
        {
            _internalSong = song;
        }

        public Song GetSong()
        {
            return _internalSong;
        }

    }
}
