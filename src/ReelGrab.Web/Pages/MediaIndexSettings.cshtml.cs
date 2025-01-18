using System.Collections.Specialized;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReelGrab.Core;
using ReelGrab.Media;

namespace MyApp.Namespace
{
    public class MediaIndexSettingsModel : PageModel
    {
        public Dictionary<ReelGrab.Core.MediaIndexConfigKey, string?> Configs = [];

        public MediaIndex mediaIndex = ReelGrab.Core.Application.instance.mediaIndex;

        public async Task OnGetAsync()
        {
            Configs = await ReelGrab.Core.Application.instance.GetMediaIndexConfigAsync();
        }

        public async Task OnPostAsync()
        {
            Configs = await ReelGrab.Core.Application.instance.GetMediaIndexConfigAsync();
            Dictionary<MediaIndexConfigKey, string?> newConfigs = new();
            IFormCollection form = Request.Form;
            foreach (MediaIndexConfigKey key in Configs.Keys)
            {
                string newVal = form[key.ToString()].First().ToString();
                newConfigs[key] = string.IsNullOrEmpty(newVal) ? null : newVal;
            }
            if (newConfigs.Keys.Count > 0)
            {
                await ReelGrab.Core.Application.instance.SetMediaIndexConfigAsync(newConfigs);
                Configs = await ReelGrab.Core.Application.instance.GetMediaIndexConfigAsync();
            }
        }
    }
}
