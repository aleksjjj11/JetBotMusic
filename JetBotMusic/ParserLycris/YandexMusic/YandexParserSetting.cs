namespace JetBotMusic.ParserLycris.YandexMusic
{
    public class YandexParserSetting : IParserSetting
    {
        public string BaseUrl { get; } = "https://music.yandex.ru";
        public string Prefix { get; } = "album/{IdAlbum}/track/{IdTrack}";
        public string TrackId { get; set; }
        public string AlbumId { get; set; }
        
        public YandexParserSetting(string albumId, string trackId)
        {
            TrackId = trackId;
            AlbumId = albumId;
        }
    }
}