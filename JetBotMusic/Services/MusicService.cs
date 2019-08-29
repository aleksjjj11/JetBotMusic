using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Genius;
using Genius.Clients;
using Genius.Models;
using JavaAnSharp;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;
using Yandex.Music.Api;
using SearchResult = Victoria.Entities.SearchResult;
using Yandex.Music.Api.Models;

namespace JetBotMusic.Services
{
    public class MusicService : ModuleBase<SocketCommandContext>
    {
        private LavaRestClient _lavaRestClient;
        private LavaSocketClient _lavaSocketClient;
        private DiscordSocketClient _client;
        private LavaPlayer _player;
        private IUserMessage _message;
        private IUserMessage _messageLyrics = null;
        public MusicService(LavaRestClient restClient, DiscordSocketClient client, LavaSocketClient socketClient)
        {
            _lavaRestClient = restClient;
            _lavaSocketClient = socketClient;
            _client = client;
        }

        public Task InitializeAsync()
        {
            _client.Ready += ClientReadyAsync;
            _lavaSocketClient.Log += LogAsync;
            _lavaSocketClient.OnTrackFinished += TrackFinished;
            _lavaSocketClient.OnPlayerUpdated += PlayerUpdated;
            return Task.CompletedTask;
        }

        private async Task PlayerUpdated(LavaPlayer player, LavaTrack track, TimeSpan timeSpan)
        {
            await TimeAsync();
        }

        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, ITextChannel textChannel)
            => await _lavaSocketClient.ConnectAsync(voiceChannel, textChannel);

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel)
            => await _lavaSocketClient.DisconnectAsync(voiceChannel);

        private async Task TimeAsync() 
        {
            TimeSpan timeSpan = _player.CurrentTrack.Position;
            LavaTrack track = _player.CurrentTrack;
            await _message.ModifyAsync(properties =>
            {
                string oldStr = _message.Embeds.First().Description.Substring(_message.Embeds.First().Description.IndexOf("**This time:**"), 
                    _message.Embeds.First().Description.IndexOf("🆒") - _message.Embeds.First().Description.IndexOf("**This time:**"));
                
                string timeMSG = default;
                //Если текущий трек является стримом, то вместо временной позиции будет отображаться надпись трансляция
                //IF this track is stream then in place of THIS TIME, inscription "STREAM" will displayed 
                if (_player.CurrentTrack.IsStream)
                {
                    timeMSG = "**This time:**`STREAMING`";
                }
                else
                {
                    string oldMinutes = timeSpan.Minutes + timeSpan.Hours * 60 < 10
                        ? '0' + (timeSpan.Minutes + timeSpan.Hours * 60).ToString()
                        : (timeSpan.Minutes + timeSpan.Hours * 60).ToString();

                    string oldSeconds = timeSpan.Seconds < 10
                        ? '0' + timeSpan.Seconds.ToString()
                        : timeSpan.Seconds.ToString();

                    string newMinutes = track.Length.Minutes + track.Length.Hours * 60 < 10
                        ? '0' + (track.Length.Minutes + track.Length.Hours * 60).ToString()
                        : (track.Length.Minutes + track.Length.Hours * 60).ToString();

                    string newSeconds = track.Length.Seconds < 10
                        ? '0' + track.Length.Seconds.ToString()
                        : track.Length.Seconds.ToString();

                    timeMSG =
                        $"**This time:**`{oldMinutes}:{oldSeconds}/{newMinutes}:{newSeconds}`";
                }
                
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle(_message.Embeds.First().Title)
                    .WithDescription(_message.Embeds.First().Description.Replace(oldStr, timeMSG))
                    .WithColor(_message.Embeds.First().Color.Value);
                
                properties.Embed = builder.Build();
            });
        }
        
        public void SetMessage(IUserMessage message)
        {
            _message = message;
        }

        public async Task GetLyrics(string query = null)
        {
            //GeniusClient geniusClient = new GeniusClient("tjYMgUzV7LWhmWvNXPmmDpDiq0ek7MMonxfNTIRDHyz5r7Z0jQi2kFMmJWDybGKd");
            //Получем песню через API Genius
            //var searchResult = geniusClient.SearchClient.Search(TextFormat.Dom, query).Result;
            //searchResult.Response.First().Index
            //Song song = geniusClient.SongsClient.GetSong(TextFormat.Dom, "").Result.Response;
            if (_player.CurrentTrack != null)
            {
                query = _player.CurrentTrack.Title;
            }
            
            YandexMusicApi musicApi = new YandexMusicApi();
            var yandexTracks = musicApi.SearchTrack(query);
            if (yandexTracks is null)
            {
                Console.WriteLine("Yandex DEBUG ---------------------->> NUUUUUULL");
            }
            else
            {
                YandexTrack track = yandexTracks.First();
                //    Process cmdProc = Process.Start("java", $"-cp Testick.jar Main \"{track.Artists.First().Name} - {track.Title}\"");
                //    if (cmdProc is null)
                //    {
                //        Console.WriteLine("Fail");
                //        return;
                //    }
                //    cmdProc.WaitForExit();

                //}*/
                ParserGenius parser = new ParserGenius($"{track.Artists.First().Name} - {track.Title}");
                parser.Initialization().Wait();
                string lyrics = null;
                lyrics = parser.GetLyrics();
                Console.WriteLine($"lyrcs------------->{lyrics}");
                if (_messageLyrics is null)
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithTitle("Lyrics")
                        .WithDescription(lyrics)
                        .WithColor(Color.Red);

                    _messageLyrics = await _player.TextChannel.SendMessageAsync("", false, builder.Build());
                }
                else
                {
                    _messageLyrics.ModifyAsync(properties => { });
                }
            }
        }
        public async Task<string> PlayAsync(string query, ulong guildId)
        {
            _player = _lavaSocketClient.GetPlayer(guildId);
            
            SearchResult results = await _lavaRestClient.SearchYouTubeAsync(query);
            if (results.LoadType == LoadType.NoMatches || results.LoadType == LoadType.LoadFailed)
            {
                return "No matches found.";
            }
            
            var track = results.Tracks.FirstOrDefault();
            
            if (_player.IsPlaying)
            {
                _player.Queue.Enqueue(track);
                return $"{track.Title} has been added to the queue.";
            }
            else
            {
                await _player.PlayAsync(track);
                return $"**Playing** `{track.Title}`";
            }
        }

        public async Task StopAsync()
        {
            if (_player is null) return;
            await _player.StopAsync();
            
            await TrackListAsync();
        }

        public async Task MuteAsync()
        {
            await _player.SetVolumeAsync(0);
        }

        public async Task UnmuteAsync()
        {
            await _player.SetVolumeAsync(100);   
        }
        public async Task SkipAsync()
        {
            if (_player is null || _player.Queue.Count is 0)
            {
                return;
            }
            
            LavaTrack oldTrack = _player.CurrentTrack;
            await _player.SkipAsync();
                
            await _message.ModifyAsync(properties =>
            {
                EmbedBuilder builder = new EmbedBuilder();
                string description = _message.Embeds.First().Description.Replace(oldTrack.Title, _player.CurrentTrack.Title);
                builder.WithTitle(_message.Embeds.First().Title)
                    .WithDescription(description)
                    .WithColor(Color.Orange);
                properties.Embed = builder.Build();
            });
            
            await TrackListAsync();
        }

        public async Task TrackListAsync()
        {
            string listMessage = "";
            string useless = _message.Embeds.First().Description.Substring(
                _message.Embeds.First().Description.IndexOf("\n🎶"),
                _message.Embeds.First().Description.Length - _message.Embeds.First().Description.IndexOf("\n🎶"));
            
            var trackList = _player.Queue.Items.ToList();

            if (_player.Queue.Count > 0)
            {
                listMessage += "\n🎶**Track in queue:**";
                for (int i = 0; i < trackList.Count; i++)
                {
                    var track = trackList[i] as LavaTrack;
                    
                    if (track is null) 
                        listMessage += "\n`Track empty`";
                    else 
                        listMessage += $"\n```css\n .{i} [{track.Title}]```";
                }
            }
            else
            {
                listMessage = "\n🎶**Track in queue:**\n***Nothing***";
            }
            
            await _message.ModifyAsync(properties =>
            {
                listMessage = _message.Embeds.First().Description.Replace(useless, " ") + listMessage; 

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle(_message.Embeds.First().Title)
                    .WithDescription(listMessage)
                    .WithColor(Color.LightOrange);
                
                properties.Embed = builder.Build();
            });
        }

        public async Task LyricsAsync()
        {
            //Not working 
            string lyrics = await _player.CurrentTrack.FetchLyricsAsync();
            if (lyrics is null)
            {
                Console.WriteLine("--------------------------> NULL");
            }
            else
            {
                Console.WriteLine($"---------------------------> {lyrics}");
            }
        }

        public async Task SeekAsync(int days = 0, int hours = 0, int minutes = 0, int second = 0)
        {
            TimeSpan time = new TimeSpan(days, hours, minutes, second);
            await _player.SeekAsync(time);
            await _player.TextChannel.SendMessageAsync();///////////////////////////////////////////////////////////////////////////////////////////////////////////
        }
        public async Task Shuffle()
        {
            _player.Queue.Shuffle();
            await TrackListAsync();
        }
        public async Task PauseAsync()
        {
            if (_player is null) return;
            if (!_player.IsPaused)
            {
                await _player.PauseAsync();
                
                //Embed embed = _message.Embeds.First();
                string firstString = _message.Embeds.First().Description.Substring(0, _message.Embeds.First().Description.IndexOf("\n"));
                
                await _message.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = _message.Embeds.First().Description.Replace(firstString, $"*Status*: **Pausing** `{_player.CurrentTrack.Title}`");
                    builder.WithTitle(_message.Embeds.First().Title)
                        .WithDescription(description)
                        .WithColor(Color.Orange);
                    properties.Embed = builder.Build();
                });
            }
            else
            {
                await ResumeAsync();
            }
        }

        public async Task ResumeAsync()
        {
            if (_player is null) return;
            if (_player.IsPaused)
            {
                await _player.ResumeAsync();
                
                string firstString = _message.Embeds.First().Description.Substring(0, _message.Embeds.First().Description.IndexOf("\n"));
                
                await _message.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = _message.Embeds.First().Description.Replace(firstString, $"*Status*: **Playing** `{_player.CurrentTrack.Title}`");
                    builder.WithTitle(_message.Embeds.First().Title)
                        .WithDescription(description)
                        .WithColor(Color.Orange);
                    properties.Embed = builder.Build();
                });
            }
            else
            {
                await PauseAsync();
            }
        }

        public async Task SetVolumeAsync(int value)
        {
            if (value > 150 || value < 0)
            {
                await _player.TextChannel.SendMessageAsync("Incorrect value for volume");
                return;
            }

            await _player.SetVolumeAsync(value);
        }

        public async Task UpVolumeAsync()
        {
            if (_player.CurrentVolume + 10 <= 150)
                await _player.SetVolumeAsync(_player.CurrentVolume + 10);
            else
                await _player.SetVolumeAsync(150);
        }

        public async Task DownVolumeAsync()
        {
            if (_player.CurrentVolume - 10 >= 0)
                await _player.SetVolumeAsync(_player.CurrentVolume - 10);
            else
                await _player.SetVolumeAsync(0);
        }

        public async Task MoveAsync(int numberTrack, int newPosition = 0)
        {
            //Если номер трека совпадает с его позицией, то ничего не делаем
            if (numberTrack == newPosition || _player.Queue.Count is 0) return;
            if (numberTrack >= _player.Queue.Count || newPosition >= _player.Queue.Count) return;
            //Получаем нашу очередь в лист 
            List<IQueueObject> queue = _player.Queue.Items.ToList();
            //Добаляем трек в новую позицию и удаляем со старой
            queue.Insert(newPosition, queue[numberTrack]);
            if (newPosition < numberTrack)
                queue.RemoveAt(numberTrack + 1);
            else 
                queue.RemoveAt(numberTrack);
            //Очищаем очередь
            _player.Queue.Clear();
            //ЗАполняем изменённую очередь
            foreach (IQueueObject element in queue)
            {
                _player.Queue.Enqueue(element);
            }

            await TrackListAsync();
        }
        private async Task TrackFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            string firstString = _message.Embeds.First().Description.Substring(0, _message.Embeds.First().Description.IndexOf("\n"));

            if (!reason.ShouldPlayNext()) return;
            if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTack))
            {
                //await player.TextChannel.SendMessageAsync("There are no more tracks in the queue");
                await _message.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = _message.Embeds.First().Description
                        .Replace(firstString, $"*Status*: **Played** `{track.Title}`");
                    
                    builder.WithTitle(_message.Embeds.First().Title)
                        .WithDescription(description)
                        .WithColor(Color.LightOrange);
                    
                    properties.Embed = builder.Build();
                });
                return;
            }
 
            await _message.ModifyAsync(properties =>
            {
                EmbedBuilder builder = new EmbedBuilder();
                string description = _message.Embeds.First().Description.Replace(firstString, $"*Status*: **Playing** `{nextTack.Title}`");
                builder.WithTitle(_message.Embeds.First().Title)
                    .WithDescription(description)
                    .WithColor(Color.Orange);
                properties.Embed = builder.Build();
            });
            await TrackListAsync();
            await player.PlayAsync(nextTack);
        }
        
        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.Message);
            return Task.CompletedTask;
        }

        private async Task ClientReadyAsync()
        {
            await _lavaSocketClient.StartAsync(_client, new Configuration
            {
                LogSeverity = LogSeverity.Info
            });
        }
    }
}