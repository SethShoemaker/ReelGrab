using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReelGrab.Media;
using ReelGrab.Core;

namespace ReelGrab.Web.Pages;

public class MediaSearchModel : PageModel
{
    private readonly ILogger<MediaSearchModel> _logger;

    [FromQuery]
    public string Q { get; set; } = "";

    public bool HasQuery => !string.IsNullOrWhiteSpace(Q);

    public List<SearchResult> Results { get; private set; } = new();

    public MediaSearchModel(ILogger<MediaSearchModel> logger)
    {
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        if(HasQuery){
            Results = await Application.instance.SearchMediaIndexByQueryAsync(Q);
        }
    }
}
