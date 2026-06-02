using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace NeonStore
{
    public static class UpdateDialog
    {
        public static async Task<bool> ShowAsync(string title, string message, string changelog)
        {
            var dialog = new MessageDialog(message + "\n\nChanges:\n" + changelog, title);

            dialog.Commands.Add(new UICommand("Update"));
            dialog.Commands.Add(new UICommand("Later"));

            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;

            var result = await dialog.ShowAsync();

            return result.Label == "Update";
        }
    }
}
