using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Genius;
using Genius.Models;

namespace JavaAnSharp
{
    public class ParserGenius
    {
        private string writters { get; } = null;
        private string lyrics = null;
        private string writtenBy { get; } = null;
        private string dateRelease { get; } = null;
        private string videoUrl { get; } = null;
        private string fullAddress { get; } = "https://genius.com/";

        public ParserGenius(string query = "NEFFEX - Trust me")
        {
            GeniusClient geniusClient = new GeniusClient("0KELsTGebL5A6ZExHu-h4sLL7MlZxoX_4DKfGgXC1RrilwKQhWpGeMcXik0hMZ35");
            //Получаем результаты по запросу
            var result = geniusClient.SearchClient.Search(TextFormat.Dom, query);
            Hit hit = result.Result.Response.First();
            string str = hit.Result.ToString();
            //Создаём устойчивое выражение, чтобы найти id нашей песни 
            Regex regex = new Regex(@"(\W)id(\W):\s\d*");
            MatchCollection collection = regex.Matches(str);
            Match matches = collection.First();

            Console.WriteLine($"Found this id: {matches.Value}");
            string id = matches.Value.Remove(0, 6);
            //По пулученному id получаем песню
            Song song = geniusClient.SongsClient.GetSong(TextFormat.Dom, id).Result.Response;
            
            if (song is null)
            {
                Console.WriteLine("Song is null, next actions stopped.");
                return;
            }
            
            Console.WriteLine($"URl : {song.Url}");
            fullAddress = song.Url is null ? null : song.Url;

            if (fullAddress is null)
            {
                Console.WriteLine("Address is null, next actions stopped.");
                return;
            }
            
        }

        public async Task Initialization()
        {
            HttpClient httpClient = new HttpClient();
            var responce = await httpClient.GetAsync(fullAddress);
            string source = null;
            source = await responce.Content.ReadAsStringAsync();
            var domParser = new HtmlParser();
            var document = await domParser.ParseDocumentAsync(source);
            //Console.WriteLine($"URl : {fullAddress}");
            var results = document.QuerySelectorAll("p");
            if (results is null) return;
            lyrics = results.First().TextContent;    
            Regex squareBrackets = new Regex(@"\[\w*\W*\w*\W*\w*\]");
            MatchCollection collection = squareBrackets.Matches(lyrics);
            foreach (Match match in collection)
            {
                lyrics = lyrics.Replace(match.Value + "\n", "");
            }
            Console.WriteLine(lyrics);
        }

        public string GetLyrics()
        {
            return lyrics;
        }

        public string[] DivideLyrics()
        {
            string[] listLyrics = new string[2];
            if (lyrics.Length > 2500)
            {
                int i = 2500;
                while (i < lyrics.Length)
                {
                    lyrics = lyrics.Insert(i, "/");
                    i += 2500;
                }

                listLyrics = lyrics.Split("/");
            }

            return listLyrics;
        }
    }
}