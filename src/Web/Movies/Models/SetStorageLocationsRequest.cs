using System.ComponentModel.DataAnnotations;

namespace ReelGrab.Web.Movies;

public class SetStorageLocationsRequest
{
    [Required]
    public List<string> StorageLocations { get; set; } = null!;
}