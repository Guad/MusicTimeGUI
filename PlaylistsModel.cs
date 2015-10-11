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
        // Your context has been configured to use a 'PlaylistsModel' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'MusicTimeGUI.PlaylistsModel' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'PlaylistsModel' 
        // connection string in the application configuration file.
        public PlaylistsModel()
            : base("name=PlaylistsModel")
        {
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        
        public virtual DbSet<Playlist> Playlists { get; set; }
    }

    public class Playlist
    {
        public List<Song> Songs { get; set; }
        public string Name { get; set; }
    }
}