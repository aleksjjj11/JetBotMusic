using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;

namespace JetBotMusic.ParserLycris
{
    public class ParserWorker<T> where T : class
    {
        IParser<T> parser;
        IParserSetting parserSettings;

        HtmlLoader loader;

        bool isActive;

        public string sourceA;

        #region Properties

        public IParser<T> Parser
        {
            get
            {
                return parser;
            }
            set
            {
                parser = value;
            }
        }
        
        public IParserSetting Settings
        {
            get
            {
                return parserSettings;
            }
            set
            {
                parserSettings = value;
                loader = new HtmlLoader(value);
            }
        }
        
        public bool IsActive
        {
            get 
            { 
                return isActive;
            }
        }

        #endregion
        
        public event Action<object, T> OnNewData;
        public event Action<object> OnCompleted;
        
        public ParserWorker(IParser<T> parser)
        {
            this.parser = parser;
        }

        public ParserWorker(IParser<T> parser, IParserSetting parserSettings) : this(parser)
        {
            this.parserSettings = parserSettings;
        }
        
        public void Start()
        {
            isActive = true;
            //Worker();
        }

        public void Abort()
        {
            isActive = false;
        }
        
        public async Task<string> Worker()
        {
            if (!isActive)
                {
                    OnCompleted?.Invoke(this);
                    return null;
                }

            var source = await loader.GetSourceByAlbumAndTrackId(parserSettings.AlbumId, parserSettings.TrackId);
            var domParser = new HtmlParser();

            var document = await domParser.ParseDocumentAsync(source);
            sourceA = source;
            
            string lyrics;
            var items = document.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("sidebar-track__lyric-text"));

            lyrics = items.First().TextContent;
            return lyrics;

            //OnNewData?.Invoke(this, result);

            //OnCompleted?.Invoke(this);
            //isActive = false;

            //return result as string;
        }
    }
}