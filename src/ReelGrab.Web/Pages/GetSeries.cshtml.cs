using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReelGrab.Core;
using ReelGrab.Media;

namespace MyApp.Namespace
{
    public class GetSeriesModel : PageModel
    {

        [FromQuery]
        public string ImdbId { get; set; } = "";

        public SeriesDetails Details = null!;

        public async Task OnGetAsync()
        {
            if(string.IsNullOrWhiteSpace(ImdbId)){
                Response.Redirect("/MediaSearch");
                return;
            }
            Details = await Application.instance.GetSeriesDetailsAsync(ImdbId);
        }
    }
}
