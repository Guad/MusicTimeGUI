using System.Collections.Generic;
using System.Windows.Documents;
using MusicTimeCore;

namespace MusicTimeGUI
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class PlaylistsModel : DbContext
    {
        public PlaylistsModel()
            : base("name=PlaylistsModel")
        {
        }
        
        
        //public virtual DbSet<Playlist> Playlists { get; set; }
    }
    /*
    public class Playlist
    {
        public int Id { get; set; }
        public List<SavedSong> Songs { get; set; }
        public string Name { get; set; }
    }

    public class SavedSong
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Uri { get; set; }
    }*/

    public class Playlist
    {
        public List<SavedSong> Songs { get; set; }
        public string Name { get; set; }
    }

    public class SavedSong
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Uri { get; set; }
    }
}