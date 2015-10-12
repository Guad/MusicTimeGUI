using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace MusicTimeGUI
{
    public static class PlaylistSaver
    {
        public static List<Playlist> Data;

        public static void LoadPlaylists()
        {
            Data = new List<Playlist>();

            if (!File.Exists("Playlists.xml"))
                return;
            
            XmlSerializer serializer = new XmlSerializer(typeof(PlaylistsFile));
            var stream = File.OpenRead("Playlists.xml");
            var play = (PlaylistsFile) serializer.Deserialize(stream);
            stream.Close();
            foreach (Playlist playlist in play.Playlists)
            {
                Data.Add(playlist);
            }
        }

        public static void SaveAll()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PlaylistsFile));
            var pFile = new PlaylistsFile();
            pFile.Playlists = new List<Playlist>(Data);
            const string path = "Playlists.xml";
            if (File.Exists(path))
                File.Delete(path);
            var stream = File.OpenWrite(path);
            serializer.Serialize(stream, pFile);
            stream.Close();
        }
    }

    public class PlaylistsFile
    {
        public List<Playlist> Playlists { get; set; }
    }

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