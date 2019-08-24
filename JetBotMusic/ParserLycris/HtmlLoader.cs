using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace JetBotMusic.ParserLycris
{
    public class HtmlLoader
    {
        private readonly HttpClient _client;
        private readonly string url;

        public HtmlLoader(IParserSetting setting)
        {
            _client = new HttpClient();
            url = $"{setting.BaseUrl}/{setting.Prefix}";
        }

        public async Task<string> GetSourceByAlbumAndTrackId(string albumId, string trackId)
        {
            var currentUrl = url.Replace("{IdAlbum}", albumId);
            currentUrl = currentUrl.Replace("{IdTrack}", trackId);

            string source = null;
            var response = await _client.GetAsync(currentUrl);
            
            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                source = await response.Content.ReadAsStringAsync();
            }

            return source;
        }
    }
}