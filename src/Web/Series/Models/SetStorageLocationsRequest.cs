using System.ComponentModel.DataAnnotations;

namespace ReelGrab.Web.Series;

public class SetStorageLocationsRequest
{
    [Required]
    public List<string> StorageLocations { get; set; } = null!;
}