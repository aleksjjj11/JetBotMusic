namespace JetBotMusic.ParserLycris
{
    public interface IParserSetting
    {
        string BaseUrl { get;}
        string Prefix { get;}
        string TrackId { get; set; }
        string AlbumId { get; set; }
    }
}