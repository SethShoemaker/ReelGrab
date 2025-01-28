namespace ReelGrab.Torrents;

public enum Category
{
    // Console
    CONSOLE = 1000,
    CONSOLE_NDS = 1010,
    CONSOLE_PSP = 1020,
    CONSOLE_WII = 1030,
    CONSOLE_XBOX = 1040,
    CONSOLE_XBOX_360 = 1050,
    CONSOLE_PS3 = 1080,
    CONSOLE_OTHER = 1090,
    CONSOLE_3DS = 1110,
    CONSOLE_PS4 = 1180,

    // Movies
    MOVIES = 2000,
    MOVIES_FOREIGN = 2010,
    MOVIES_SD = 2030,
    MOVIES_HD = 2040,
    MOVIES_UHD = 2050,
    MOVIES_3D = 2060,
    MOVIES_DVD = 2070,

    // Audio
    AUDIO = 3000,
    AUDIO_MP3 = 3010,
    AUDIO_VIDEO = 3020,
    AUDIO_AUDIOBOOK = 3030,
    AUDIO_LOSSLESS = 3040,
    AUDIO_OTHER = 3050,

    // PC
    PC = 4000,
    PC_MAC = 4030,
    PC_MOBILE_OTHER = 4040,
    PC_GAMES = 4050,
    PC_MOBILE_IOS = 4060,
    PC_MOBILE_ANDROID = 4070,

    // TV
    TV = 5000,
    TV_SD = 5030,
    TV_HD = 5040,
    TV_ANIME = 5070,
    TV_DOCUMENTARY = 5080,

    // XXX
    XXX = 6000,
    XXX_DVD = 6010,
    XXX_IMAGESET = 6060,

    // Books
    BOOKS = 7000,
    BOOKS_EBOOK = 7020,
    BOOKS_COMICS = 7030,

    // Other
    OTHER = 8000,
    OTHER_MISC = 8010
}