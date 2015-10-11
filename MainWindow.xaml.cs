using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro;
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
            PlaylistSaver.LoadPlaylists();
            _settings = Settings.Load();
            Timer t = new Timer(1000);
            t.Elapsed += Tick;
            t.Start();
            InitializePlaylists();

            ThemeToggleSwitch.IsChecked = _settings.BaseTheme == "BaseLight";
            ComboBoxItem ourItem = new ComboBoxItem();
            for (int i = 0; i < SettingsAccentComboBox.Items.Count; i++)
            {
                if (((ComboBoxItem)SettingsAccentComboBox.Items[i]).Content.ToString() == _settings.Accent)
                    ourItem = (ComboBoxItem)SettingsAccentComboBox.Items[i];
            }
            SettingsAccentComboBox.SelectedIndex = SettingsAccentComboBox.Items.IndexOf(ourItem);
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            if (_hotPlaylist)
            {
                PlaylistSaver.SaveAll();
                _hotPlaylist = false;
            }

            if (!_isSongPlaying) return;
            if (manager.Finished)
            {
                if (_currentSong >= _history.Count - 1)
                {
                    _isSongPlaying = false;
                    Dispatcher.BeginInvoke((Action) delegate
                    {
                        Title = "MusicTime";
                    });
                    return;
                }
                PlaySong(_history[_currentSong++]);
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
                    NextRect.Fill = Brushes.Gray;
                }
                else
                {
                    NextButton.IsEnabled = true;
                    NextRect.Fill = ThemeManager.DetectAppStyle(Application.Current).Item1 == ThemeManager.GetAppTheme("BaseDark") ? Brushes.White : Brushes.Black;
                }

                if ((_currentSong == -1 || _currentSong == 0) && percentage < 0.05d)
                {
                    PrevButton.IsEnabled = false;
                    PrevRect.Fill = Brushes.Gray;
                }
                else
                {
                    PrevButton.IsEnabled = true;
                    PrevRect.Fill = ThemeManager.DetectAppStyle(Application.Current).Item1 == ThemeManager.GetAppTheme("BaseDark") ? Brushes.White : Brushes.Black;
                }
            });

            
        }

        private bool _isSongPlaying;
        private bool _isPaused;
        private List<KnownSongInfo> _history;
        private int _currentSong = -1;

        private Settings _settings;
        private Playlist _currentPlaylist;
        private Playlist _shownPlaylist;
        private Button _shownPlaylistButton;
        private bool _hotPlaylist;
        
        private bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                //PlayPauseRectangle.Fill = new VisualBrush() // TODO: Fix this, it doesnt work.
                //{
                    //Visual = value ? (Visual) Resources["appbar_control_play"] : (Visual) Resources["appbar_control_pause"],
                //};
            }
        }
        
        private AudioManager manager;
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string query = textBox.Text;
            if (string.IsNullOrWhiteSpace(query)) return;
            Thread searchThread = new Thread((ThreadStart) delegate
            {
                SearchProgressRing.Dispatcher.BeginInvoke((Action) delegate
                {
                    SearchProgressRing.IsActive = true;
                    textBox.IsEnabled = false;
                    searchButton.IsEnabled = false;
                    SearchResultsGrid.IsEnabled = false;
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
                SearchProgressRing.Dispatcher.BeginInvoke((Action)delegate
                {
                    SearchProgressRing.IsActive = false;
                    textBox.IsEnabled = true;
                    searchButton.IsEnabled = true;
                    SearchResultsGrid.IsEnabled = true;
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
            songPicture.Source = new BitmapImage(new Uri("DefaultAlbum.png", UriKind.Relative));
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
                    // No album cover found.
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

        private void DisplayPlaylist(Playlist list)
        {
            MainTabControl.SelectedItem = PlaylistTabItem;
            PlaylistNameTextbox.Text = list.Name;

            ObservableCollection<KnownSongInfo> songs = new ObservableCollection<KnownSongInfo>();
            foreach (SavedSong song in list.Songs)
            {
                var s = new KnownSongInfo() { Artist = song.Artist, Name = song.Name };
                s.SetSong(new Song()
                {
                    Artist = song.Artist,
                    Name = song.Name,
                    Uri = song.Uri,
                });
                songs.Add(s);
            }
            CurrentPlaylistDataGrid.ItemsSource = songs;
            _shownPlaylist = list;
        }

        private Button CreatePlaylistButton()
        {
            Button btn = new Button();
            btn.MinHeight = 20;
            btn.FontSize = 16;
            btn.Margin = new Thickness(10);
            return btn;
        }

        private Dictionary<Playlist, PlaylistData> _mainPlaylistData = new Dictionary<Playlist, PlaylistData>();

        private void InitializePlaylists()
        {
            foreach (Playlist playlist in PlaylistSaver.Data)
            {
                var btn = CreatePlaylistButton();
                btn.Content = playlist.Name;
                btn.Click += (s, e) => 
                {
                    DisplayPlaylist(playlist);
                    _shownPlaylistButton = btn;
                };
                PlaylistStackPanel.Children.Insert(PlaylistStackPanel.Children.Count - 1, btn);
                var mi = new MenuItem();
                mi.Header = "Add to " + playlist.Name;
                mi.Click += MenuItem_OnClick;
                SearchContextMenu.Items.Add(mi);

                _mainPlaylistData.Add(playlist, new PlaylistData()
                {
                    AssociatedButton = btn,
                    AssociatedContextItem = mi,
                });

                if(playlist.Songs == null)
                    playlist.Songs = new List<SavedSong>();
            }
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            // Add to playlist
            var menuItem = (MenuItem) sender;

            var contextMenu = (ContextMenu) menuItem.Parent;
            var row = (DataGrid)contextMenu.PlacementTarget;
            var item = (KnownSongInfo)row.SelectedCells[0].Item;
            var playlist = _mainPlaylistData.FirstOrDefault(p => Equals(p.Value.AssociatedContextItem, menuItem)).Key;
            if (playlist == null)
            {
                MessageBox.Show("Error while looking for playlist.");
                return;
            }

            var songData = new SavedSong()
            {
                Artist = item.Artist,
                Name = item.Name,
                Uri = item.GetSong().Uri,
            };


            var data = PlaylistSaver.Data.First(p => p.Equals(playlist));
            if(data.Songs == null)
                data.Songs = new List<SavedSong> {songData};
            else
                data.Songs.Add(songData);
            PlaylistSaver.SaveAll();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //Create new playlist
            Playlist playlist = new Playlist();
            playlist.Name = "New Playlist";
            playlist.Songs = new List<SavedSong>();
            PlaylistSaver.Data.Add(playlist);
            PlaylistSaver.SaveAll();

            var mi = new MenuItem();
            mi.Header = "Add to " + playlist.Name;
            mi.Click += MenuItem_OnClick;
            SearchContextMenu.Items.Add(mi);

            var btn = CreatePlaylistButton();
            btn.Content = playlist.Name;
            btn.Click += (s, _) =>
            {
                _shownPlaylistButton = (Button) s;
                DisplayPlaylist(playlist);
            };

            _mainPlaylistData.Add(playlist, new PlaylistData()
            {
                AssociatedButton = btn,
                AssociatedContextItem = mi,
            });

            _shownPlaylist = playlist;
            _shownPlaylistButton = btn;
            PlaylistStackPanel.Children.Insert(PlaylistStackPanel.Children.Count-1,btn);
        }

        private void PlaylistContextMenu_OnClick(object sender, RoutedEventArgs e)
        {
            if(_shownPlaylist == null) return;
            var menuItem = (MenuItem)sender;

            var contextMenu = (ContextMenu)menuItem.Parent;
            var row = (DataGrid)contextMenu.PlacementTarget;
            var item = (KnownSongInfo)row.SelectedCells[0].Item;
            
            var data = PlaylistSaver.Data.First(p => p.Equals(_shownPlaylist));
            var song = data.Songs.First(s => s.Uri == item.GetSong().Uri);
            data.Songs.Remove(song);
            PlaylistSaver.SaveAll();
            DisplayPlaylist(data);
        }

        private void DiscoverButton_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedItem = SearchTabItem;
            _shownPlaylist = null;
            _shownPlaylistButton = null;
        }

        private void VisitGithub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Guad/MusicTimeGUI");
        }

        private void PlaylistNameTextbox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (_shownPlaylist != null && ((TextBox)sender).IsFocused)
            {
                var playlistToSave = PlaylistSaver.Data.First(p => p.Equals(_shownPlaylist));
                playlistToSave.Name = PlaylistNameTextbox.Text;
                _hotPlaylist = true;

                if (_shownPlaylistButton != null)
                    _shownPlaylistButton.Content = PlaylistNameTextbox.Text;
                    
                _mainPlaylistData[_shownPlaylist].AssociatedContextItem.Header = "Add to " + PlaylistNameTextbox.Text;
            }
        }

        private void PlaylistDataGrid_MouseDoubleclick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null) return;
            var item = CurrentPlaylistDataGrid.Items[row.GetIndex()] as KnownSongInfo;
            if (item == null) return;
            PlaySong(item);
            if (_currentSong != -1 && _currentSong != _history.Count - 1)
            {
                _history.RemoveRange(_currentSong, _history.Count - _currentSong);
            }
            _history.Add(item);
            _currentSong = _history.Count - 1;
        }

        private void PlayPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (_shownPlaylist == null) return;
            if (_currentSong != -1 && _currentSong != _history.Count - 1)
            {
                _history.RemoveRange(_currentSong, _history.Count - _currentSong);
            }
            var pos = _history.Count;
            foreach (var song in _shownPlaylist.Songs)
            {
                var s = new KnownSongInfo()
                {
                    Artist = song.Artist,
                    Name = song.Name,
                };
                s.SetSong(new Song()
                {
                    Artist = song.Artist,
                    Name = song.Name,
                    Uri = song.Uri,
                });
                _history.Add(s);
            }
            _currentSong = pos;
            PlaySong(_history[pos]);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
        }

        private void SettingsThemeSwitch_Click(object sender, RoutedEventArgs e)
        {
            _settings.BaseTheme = ThemeToggleSwitch.IsChecked.HasValue ? ThemeToggleSwitch.IsChecked.Value ? "BaseLight" : "BaseDark" : "BaseDark";
            Settings.Save(_settings);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _settings.Accent = ((ComboBoxItem)((ComboBox) sender).SelectedItem).Content.ToString();
            Settings.Save(_settings);
        }

        private void DeletePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            PlaylistStackPanel.Children.Remove(_shownPlaylistButton);
            SearchContextMenu.Items.Remove(_mainPlaylistData[_shownPlaylist].AssociatedContextItem);

            PlaylistSaver.Data.Remove(_shownPlaylist);
            PlaylistSaver.SaveAll();
            MainTabControl.SelectedItem = SearchTabItem;
            _shownPlaylist = null;
            _shownPlaylistButton = null;
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

public class PlaylistData
{
    public Button AssociatedButton { get; set; }
    public MenuItem AssociatedContextItem { get; set; }
}