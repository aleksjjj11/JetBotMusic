using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Genius;
using Genius.Models;

namespace JavaAnSharp
{
    public class ParserGenius
    {
        private string _lyrics = "";
        private readonly string _fullAddress = "https://genius.com/";

        public ParserGenius(string query = "NEFFEX - Trust me")
        {
            var geniusClient = new GeniusClient("0KELsTGebL5A6ZExHu-h4sLL7MlZxoX_4DKfGgXC1RrilwKQhWpGeMcXik0hMZ35");
            var result = geniusClient.SearchClient.Search(TextFormat.Dom, query);
            var firstHit = result.Result.Response.First();
            var str = firstHit.Result.ToString();

            //Создаём устойчивое выражение, чтобы найти id нашей песни 
            var regex = new Regex(@"(\W)id(\W):\s\d*");
            var collection = regex.Matches(str ?? string.Empty);
            var matches = collection.First();

            Console.WriteLine($"Found this id: {matches.Value}");

            var id = matches.Value.Remove(0, 6);

            //По пулученному id получаем песню
            var song = geniusClient.SongsClient.GetSong(TextFormat.Dom, id).Result.Response;

            if (song is null)
            {
                Console.WriteLine("Song is null, next actions stopped.");
                return;
            }

            Console.WriteLine($"URl : {song.Url}");

            _fullAddress = song.Url;

            if (_fullAddress is null)
            {
                Console.WriteLine("Address is null, next actions stopped.");
                return;
            }
        }

        public async Task Initialization()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(_fullAddress);

            var source = await response.Content.ReadAsStringAsync();

            var domParser = new HtmlParser();
            var document = await domParser.ParseDocumentAsync(source);

            var results = document.QuerySelectorAll("p");

            if (results is null) return;

            _lyrics = results.First().TextContent;

            var squareBrackets = new Regex(@"\[\w*\W*\w*\W*\w*\]");
            var collection = squareBrackets.Matches(_lyrics);

            foreach (Match match in collection)
            {
                _lyrics = _lyrics.Replace(match.Value + "\n", "");
            }
        }

        public string GetLyrics()
        {
            return _lyrics;
        }

        public List<string> DivideLyrics()
        {
            var listLyrics = new List<string>();

            var i = 2000;

            while (i < _lyrics.Length)
            {
                _lyrics = _lyrics.Insert(i, "/");
                i += 2000;
            }

            listLyrics.AddRange(_lyrics.Split("/"));

            return listLyrics;
        }
    }
}