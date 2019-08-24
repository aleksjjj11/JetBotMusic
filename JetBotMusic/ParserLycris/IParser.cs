using AngleSharp.Html.Dom;

namespace JetBotMusic.ParserLycris
{
    public interface IParser<T> where T : class
    {
        T Parse(IHtmlDocument document);
    }
}