using System.IO;
using System.Xml.Serialization;

namespace MusicTimeGUI
{
    public class Settings
    {
        public string BaseTheme { get; set; }
        public string Accent { get; set; }

        public static void Save(Settings settings)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            if(File.Exists("Settings.xml"))
                File.Delete("Settings.xml");
            var stream = File.OpenWrite("Settings.xml");
            serializer.Serialize(stream, settings);
            stream.Close();
        }

        public static Settings Load()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            if (!File.Exists("Settings.xml"))
            {
                return new Settings()
                {
                    Accent = "Blue",
                    BaseTheme = "BaseLight",
                };
            }

            var stream = File.OpenRead("Settings.xml");
            var settings = (Settings)serializer.Deserialize(stream);
            stream.Close();
            return settings;
        }
    }
}