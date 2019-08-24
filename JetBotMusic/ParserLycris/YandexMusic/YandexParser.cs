using System.Linq;
using AngleSharp.Html.Dom;

namespace JetBotMusic.ParserLycris.YandexMusic
{
    public class YandexParser : IParser<string>
    {
        public string Parse(IHtmlDocument document)
        {
            string lyrics;
            var items = document.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("sidebar-track__lyric-text"));

            lyrics = items.First().TextContent;
            return lyrics;
        }
    }
}