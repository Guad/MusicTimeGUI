using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro;

namespace MusicTimeGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var theme = ThemeManager.DetectAppStyle(Application.Current);

            var settings = Settings.Load();

            // now set the Green accent and dark theme
            ThemeManager.ChangeAppStyle(Application.Current,
                                        ThemeManager.GetAccent(settings.Accent),
                                        ThemeManager.GetAppTheme(settings.BaseTheme));
            base.OnStartup(e);
        }
    }
}
