namespace tests.Torrent;

using ReelGrab.Torrent;

public class MagnetLinkTest
{
    [Fact]
    public void ExtractsHexEncodedInfoHashAtTheBeginning()
    {
        Assert.Equal("5636A2254E4C94B1A2EF409A04AF733DE42A702D", MagnetLink.GetInfoHash("magnet:?xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D&dn=South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.tracker.cl%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.dstud.io%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce"));
    }

    [Fact]
    public void ExtractsHexEncodedInfoHashInTheMiddle()
    {
        Assert.Equal("5636A2254E4C94B1A2EF409A04AF733DE42A702D", MagnetLink.GetInfoHash("magnet:?dn=South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta&xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.tracker.cl%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.dstud.io%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce"));
    }

    [Fact]
    public void ExtractsHexEncodedInfoHashAtTheEnd()
    {
        Assert.Equal("5636A2254E4C94B1A2EF409A04AF733DE42A702D", MagnetLink.GetInfoHash("magnet:?dn=South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-RaptaD&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.tracker.cl%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.dstud.io%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce&xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D"));
    }

    [Fact]
    public void ExtractsDisplayNameFollowedByMore()
    {
        Assert.Equal("South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta", MagnetLink.GetDisplayNameIfPresent("magnet:?xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D&dn=South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.tracker.cl%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.dstud.io%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce"));
    }

    [Fact]
    public void ExtractsDisplayNameAtEnd()
    {
        Assert.Equal("South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta", MagnetLink.GetDisplayNameIfPresent("magnet:?xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D&dn=South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta"));
    }

    [Fact]
    public void ExtractsDisplayNameAtBeginning()
    {
        Assert.Equal("South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta", MagnetLink.GetDisplayNameIfPresent("magnet:?dn=South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta&xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.tracker.cl%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.dstud.io%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce"));
    }

    [Fact]
    public void ExtractsNullWhenNoDisplayNamePresent()
    {
        Assert.Null(MagnetLink.GetDisplayNameIfPresent("magnet:?xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.tracker.cl%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.dstud.io%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce"));
    }

    [Fact]
    public void ExtractsTrackerUrlsWithAllTrackersInMiddle()
    {
        Assert.Equal([
            "udp://tracker.opentrackr.org:1337/announce",
            "udp://open.tracker.cl:1337/announce",
            "udp://open.demonii.com:1337/announce",
            "udp://open.stealth.si:80/announce",
            "udp://tracker.torrent.eu.org:451/announce",
            "udp://exodus.desync.com:6969/announce",
            "udp://open.dstud.io:6969/announce",
            "udp://tracker.ololosh.space:6969/announce",
            "udp://explodie.org:6969/announce",
            "udp://tracker-udp.gbitt.info:80/announce"
        ], MagnetLink.GetTrackerUrls("magnet:?xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D&dn=South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.tracker.cl%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.dstud.io%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce"));
    }

    [Fact]
    public void ExtractsTrackerUrlsWithTrackerAtStart()
    {
        Assert.Equal([
            "udp://tracker.opentrackr.org:1337/announce",
            "udp://open.tracker.cl:1337/announce",
            "udp://open.demonii.com:1337/announce",
            "udp://open.stealth.si:80/announce",
            "udp://tracker.torrent.eu.org:451/announce",
            "udp://exodus.desync.com:6969/announce",
            "udp://open.dstud.io:6969/announce",
            "udp://tracker.ololosh.space:6969/announce",
            "udp://explodie.org:6969/announce",
            "udp://tracker-udp.gbitt.info:80/announce"
        ], MagnetLink.GetTrackerUrls("magnet:?tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&dn=South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta&xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D&tr=udp%3A%2F%2Fopen.tracker.cl%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.dstud.io%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce"));
    }

    [Fact]
    public void ExtractsNoTrackerUrlsWhenNonePresent()
    {
        Assert.Equal([], MagnetLink.GetTrackerUrls("magnet:?xt=urn:btih:5636A2254E4C94B1A2EF409A04AF733DE42A702D&dn=South+Park+S26+S26+1080p+Uncensored+WEBRip+DD5.1+10bits+x265-Rapta"));
    }
}