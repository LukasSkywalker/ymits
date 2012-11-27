using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ApplicationSettings;

namespace MusicBird
{
    class SettingsCharm
    {
        public static void AttachRequestHandler(SettingsPane sp)
        {
            sp.CommandsRequested += CommandsRequested;
        }

        private static void CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            args.Request.ApplicationCommands.Clear();
            SettingsCommand privacyPref = new SettingsCommand("privacyPref", "Privacy Policy", async (uiCommand) =>
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri("http://musicdc.sourceforge.net/musicbird.php"));
            });
            args.Request.ApplicationCommands.Add(privacyPref);
        }
    }
}
