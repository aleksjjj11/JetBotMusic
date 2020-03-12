using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Common;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JavaAnSharp;
using Victoria;
// using Victoria.Entities;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Interfaces;
using Victoria.Responses.Rest;
// using Victoria.Queue;
using Yandex.Music.Api;
using Yandex.Music.Api.Models;

namespace JetBotMusic.Services
{
    public class MusicService : ModuleBase<SocketCommandContext>
    {
        private LavaNode _lavaNode;
        private DiscordSocketClient _client;
        private LavaPlayer _player;
        private Queue<LavaTrack> _loopQueue;
        private LavaTrack _loopTrack;
        private IUserMessage _message;
        private IUserMessage _messageLyrics = null;
        private List<IUserMessage> _messagesLyrics = null;
        private bool _isLoopTrack = false;
        //private bool _isLoopQueue = false;

        public MusicService(LavaNode lavaNode, DiscordSocketClient client)
        {
            _lavaNode = lavaNode;
            _client = client;
        }
        public async Task OnReadyAsync() => await _lavaNode.ConnectAsync();
        public Task InitializeAsync()
        {
            _client.Ready += OnReadyAsync;            
            _lavaNode.OnLog += LogAsync;
            _lavaNode.OnPlayerUpdated += PlayerUpdated;
            _lavaNode.OnTrackEnded += TrackFinished;
            return Task.CompletedTask;
        }
        private async Task PlayerUpdated(PlayerUpdateEventArgs playerUpdateEventArgs)
        {
            await TrackListAsync();
            await UpdateVote();
            await UpdatePing();
            await TimeAsync();
            await UpdateTrack();
        }

        private async Task UpdateTrack()
        {
            string firstString = _message.Embeds.First().Description
                .Substring(0, _message.Embeds.First().Description.IndexOf("\n"));
            if (firstString.Contains(_player.Track.Title) is true) return;
            await _message.ModifyAsync(properties =>
            {
                EmbedBuilder builder = new EmbedBuilder();
                string description = _message.Embeds.First().Description.Replace(firstString,
                    $"*Status*: **{_player.PlayerState}** `{_player.Track.Title}`");
                builder.WithTitle(_message.Embeds.First().Title)
                    .WithDescription(description)
                    .WithColor(Color.Orange);
                properties.Embed = builder.Build();
            });
        }
        private async Task UpdatePing()
        {
            await _message.ModifyAsync(properties =>
            {
                string oldPing = _message.Embeds.First().Description.Substring(
                    _message.Embeds.First().Description.IndexOf("*Ping:*"),
                    _message.Embeds.First().Description.IndexOf("🛰")  - 
                    _message.Embeds.First().Description.IndexOf("*Ping:*"));
                string newPing = $"*Ping:* `{StreamMusicBot.Latency}`";
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle(_message.Embeds.First().Title)
                    .WithDescription(_message.Embeds.First().Description.Replace(oldPing, newPing))
                    .WithColor(_message.Embeds.First().Color.Value);
                properties.Embed = builder.Build();
            });
        }

        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, ITextChannel textChannel)
            => await _lavaNode.JoinAsync(voiceChannel, textChannel);

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel)
            => await _lavaNode.LeaveAsync(voiceChannel);

        private async Task TimeAsync()
        {
            Console.WriteLine(_player.PlayerState);
            TimeSpan timeSpan = _player.Track.Position;
            LavaTrack track = _player.Track;
            await _message.ModifyAsync(properties =>
            {
                string oldStr = _message.Embeds.First().Description.Substring(
                    _message.Embeds.First().Description.IndexOf("**This time:**"),
                    _message.Embeds.First().Description.IndexOf("🆒") -
                    _message.Embeds.First().Description.IndexOf("**This time:**"));

                string timeMSG = default;
                //Если текущий трек является стримом, то вместо временной позиции будет отображаться надпись трансляция
                //IF this track is stream then in place of THIS TIME, inscription "STREAM" will displayed 
                if (_player.Track.IsStream)
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
                    
                    string newMinutes = track.Duration.Minutes + track.Duration.Hours * 60 < 10
                        ? '0' + (track.Duration.Minutes + track.Duration.Hours * 60).ToString()
                        : (track.Duration.Minutes + track.Duration.Hours * 60).ToString();

                    string newSeconds = track.Duration.Seconds < 10
                        ? '0' + track.Duration.Seconds.ToString()
                        : track.Duration.Seconds.ToString();

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

        public async Task GetLyricsAsync(SocketUser user,string query = null)
        {
            //Массив строк для неопреледелённого количества сообщения для вывода текста песен
            List<string> listLyrics = new List<string>();

            if (_player.Track != null)
            {
                query = _player.Track.Title;
            }

            YandexMusicApi musicApi = new YandexMusicApi();
            var yandexTracks = musicApi.SearchTrack(query);
            if (yandexTracks is null || yandexTracks.Count < 1)
            {
                Console.WriteLine("Yandex DEBUG ---------------------->> NUUUUUULL");
                listLyrics[0] = "**NOT FOUND**";
            }
            YandexTrack track = yandexTracks is null ? null : yandexTracks.First();
            if (track is null) listLyrics[0] = "**NOT FOUND**"; 
            else
            {
                ParserGenius parser = new ParserGenius($"{track.Artists.First().Name} - {track.Title}");
                parser.Initialization().Wait();
                
                listLyrics = parser.DivideLyrics();
            }
            var dmChannel = user.GetOrCreateDMChannelAsync();
            for (int i = 0; i < listLyrics.Count; i++)
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("Lyrics")
                    .WithDescription(listLyrics[i])
                    .WithColor(Color.Red);
                await dmChannel.Result.SendMessageAsync("", false, builder.Build());
            }
        }

        public async Task<string> PlayAsync(string query, SocketGuild guild, string source = "youtube")
        {
            _player = _lavaNode.GetPlayer(guild);
            SearchResponse res;
            if (source == "youtube")
            {
                res = await _lavaNode.SearchYouTubeAsync(query);
            } 
            else if (source == "soundcloud")
            {
                res = await _lavaNode.SearchSoundCloudAsync(query);
            }
            else
            {
                res = await _lavaNode.SearchYouTubeAsync(query);
            }
            var track = res.Tracks.FirstOrDefault();

            if (_player.PlayerState == PlayerState.Playing)
            {
                _player.Queue.Enqueue(track);
                return $"{track.Title} has been added to the queue.";
            }
            
            await _player.PlayAsync(track);
            await _player.UpdateVolumeAsync(100);
            return $"**Playing** `{track.Title}`";
        }

        public async Task StopAsync()
        {
            if (_player is null) return;
            await _player.StopAsync();
            _messageLyrics = null;
            await TrackListAsync();
        }

        public async Task MuteAsync()
        {
            
            await _player.UpdateVolumeAsync(0);
        }

        public async Task UnmuteAsync()
        {
            await _player.UpdateVolumeAsync(100);
        }

        public async Task AliasAsync(SocketUser user)
        {
            EmbedBuilder builder = new EmbedBuilder();
            string message = "[!] - Prefix for control bot.\n" +
                            "[Join] - Connect bot to you.\n" +
                            "[Play] <query> - Plays a song with the given name or url.\n" +
                            "[PlaySoundCloud] <query> - too [play], but from sound cloud.\n" +
                            "[Pause] - Pauses the currently playing track.\n" +
                            "[Resume] - Resume paused music.\n" +
                            "[Stop] - Stop playing.\n" +
                            "[Lyrics] - Gets the lyrics of the current playing song.\n" +
                            "[Skip] - Skips the currently playing song.\n" +
                            "[Leave] - Disconnect the bot from the voice channel it is in.\n" +
                            "[Ping] - Checks the bot's response time to Discord.\n" +
                            "[SetVolume] - Change the current volume.\n" +
                            "[Shuffle] - Shuffles the queue.\n" +
                            "[Seek] - Seeks to a certain point in the current track.\n" +
                            "[List] - Update list of queue.\n" +
                            "[Move] - Moves a certain song to a chosen position.\n" +
                            "[Remove] - Removes a certain entry from the queue.\n" +
                            "[LoopQueue] - Loops the whole queue.\n" +
                            "[Loop] - Loop the currently playing song.\n" +
                            "[Replay] - Reset the progress of the current song.\n" +
                            "[RemoveDupes] - Removes duplicate songs from the queue.\n" +
                            "[LeaveCleanUp] - Removes absent user's songs from the Queue.\n";
            builder.WithTitle("Lyrics")
                    .WithDescription(message)
                    .WithColor(Color.Red);
            var dmChannel = user.GetOrCreateDMChannelAsync();
            await dmChannel.Result.SendMessageAsync("", false, builder.Build());
        }

        public async Task SkipAsync()
        {
            if (_player is null || _player.Queue.Count is 0)
            {
                return;
            }

            LavaTrack oldTrack = _player.Track;
            await _player.SkipAsync();

            await _message.ModifyAsync(properties =>
            {
                EmbedBuilder builder = new EmbedBuilder();
                string description = _message.Embeds.First().Description
                    .Replace(oldTrack.Title, _player.Track.Title);
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

        public async Task SeekAsync(int days = 0, int hours = 0, int minutes = 0, int second = 0)
        {
            TimeSpan time = new TimeSpan(days, hours, minutes, second);
            await _player.SeekAsync(time);
            await _player.TextChannel
                .SendMessageAsync(); 
        }

        public async Task Shuffle()
        {
            _player.Queue.Shuffle();
            await TrackListAsync();
        }

        public async Task PauseAsync()
        {
            if (_player is null) return;
            if (!(_player.PlayerState == PlayerState.Paused))
            {
                await _player.PauseAsync();

                string firstString = _message.Embeds.First().Description
                    .Substring(0, _message.Embeds.First().Description.IndexOf("\n"));

                await _message.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = _message.Embeds.First().Description.Replace(firstString,
                        $"*Status*: **Pausing** `{_player.Track.Title}`");
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
            if (_player.PlayerState == PlayerState.Paused)
            {
                await _player.ResumeAsync();

                string firstString = _message.Embeds.First().Description
                    .Substring(0, _message.Embeds.First().Description.IndexOf("\n"));

                await _message.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = _message.Embeds.First().Description.Replace(firstString,
                        $"*Status*: **Playing** `{_player.Track.Title}`");
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

        public async Task SetVolumeAsync(ushort value)
        {
            if (value > 150 || value < 0)
            {
                await _player.TextChannel.SendMessageAsync("Incorrect value for volume");
                return;
            }

            await _player.UpdateVolumeAsync(value);
        }

        public async Task UpVolumeAsync()
        {
            if (_player.Volume + 10 <= 150)
                await _player.UpdateVolumeAsync((ushort) (_player.Volume + 10));
            else
                await _player.UpdateVolumeAsync(150);
        }

        public async Task DownVolumeAsync()
        {
            if (_player.Volume - 10 >= 0)
                await _player.UpdateVolumeAsync((ushort) (_player.Volume - 10));
            else
                await _player.UpdateVolumeAsync(0);
        }

        public async Task MoveAsync(int numberTrack, int newPosition = 0)
        {
            //Если номер трека совпадает с его позицией, то ничего не делаем
            if (numberTrack == newPosition || _player.Queue.Count is 0) return;
            if (numberTrack >= _player.Queue.Count || newPosition >= _player.Queue.Count) return;
            //Получаем нашу очередь в лист 
            var queue = _player.Queue.Items.ToList();

            //Добаляем трек в новую позицию и удаляем со старой
            queue.Insert(newPosition, queue[numberTrack]);
            if (newPosition < numberTrack)
                queue.RemoveAt(numberTrack + 1);
            else
                queue.RemoveAt(numberTrack);
            //Очищаем очередь
            _player.Queue.Clear();
            //ЗАполняем изменённую очередь
            foreach (IQueueable element in queue)
            {
                _player.Queue.Enqueue(element);
            }

            await TrackListAsync();
        }

        private async Task TrackFinished(TrackEndedEventArgs trackEndedEventArgs)
        {
            var reason = trackEndedEventArgs.Reason;
            var player = trackEndedEventArgs.Player;
            var track = trackEndedEventArgs.Track;
            string firstString = _message.Embeds.First().Description
                .Substring(0, _message.Embeds.First().Description.IndexOf("\n"));

            if (!reason.ShouldPlayNext()) return;

            if (_isLoopTrack)
            {
                await _message.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = _message.Embeds.First().Description
                        .Replace(firstString, $"*Status*: **Playing** `{track.Title}`");
                    builder.WithTitle(_message.Embeds.First().Title)
                        .WithDescription(description)
                        .WithColor(Color.Orange);
                    properties.Embed = builder.Build();
                });
                await player.PlayAsync(track);
                await TrackListAsync();
                return;
            }
           
            if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTack))
            {
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
                _messageLyrics = null;
                return;
            }

            await _message.ModifyAsync(properties =>
            {
                EmbedBuilder builder = new EmbedBuilder();
                string description = _message.Embeds.First().Description
                    .Replace(firstString, $"*Status*: **Playing** `{nextTack.Title}`");
                builder.WithTitle(_message.Embeds.First().Title)
                    .WithDescription(description)
                    .WithColor(Color.Orange);
                properties.Embed = builder.Build();
            });
            //player.Queue.Enqueue(track);
            await player.PlayAsync(nextTack);
            await TrackListAsync();
        }

        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.Message);
            return Task.CompletedTask;
        }

        public async Task RemoveAsync(int index = 0)
        {
            int i = 0;
            foreach (LavaTrack element in _player.Queue.Items)
            {
                if (i == index)
                {
                    _player.Queue.Remove(element);
                    break;
                }
                i++;
            }

            await TrackListAsync();
        }

        public async Task ReplayAsync()
        {
            await _player.PlayAsync(_player.Track);
        }

        public async Task RemoveDupesAsync()
        {
            if (_player.Queue.Count is 0) return;

            for (int i = 0; i < _player.Queue.Count - 1; i++)
            {
                LavaTrack firstTrack = _player.Queue.Items.GetItemByIndex(i) as LavaTrack;
                for (int j = i + 1; j < _player.Queue.Count; j++)
                {
                    LavaTrack secondTrack = _player.Queue.Items.GetItemByIndex(j) as LavaTrack;

                    if (firstTrack?.Id == secondTrack?.Id) _player.Queue.Remove(secondTrack);
                }
            }

            await TrackListAsync();
        }

        public async Task LeaveCleanUpAsync()
        {
        }

        public async Task<bool> LoopTrackAsync()
        {
            _isLoopTrack = !_isLoopTrack;
            return _isLoopTrack;
        }

        public async Task LoopQueueAsync()
        {
            //_isLoopQueue = !_isLoopQueue;
            _loopTrack = _player.Track;
        }

        private async Task UpdateVote()
        {
            try
            {
                
                if ((_player.VoiceChannel as SocketVoiceChannel) is null)
                {
                    Console.WriteLine($"VOICE CHANNEL IS NULL");
                    return;
                }
                int amountVote = (_player.VoiceChannel as SocketVoiceChannel).Users.Count / 2 + 1;
                string newValue = $"***Need votes for skip:*** `{amountVote}`⏭";
                int startIndex = _message.Embeds.First().Description.IndexOf("***Need votes for skip:***");
                int endIndex = _message.Embeds.First().Description.IndexOf("⏭") + 1;
                string oldValue = _message.Embeds.First().Description.Substring(startIndex, endIndex-startIndex);
                Console.WriteLine($"START INDEX: {startIndex} END INDEX: {endIndex}\nNew value:{newValue}\nOld:{oldValue}");
                await _message.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = _message.Embeds.First().Description
                        .Replace(oldValue, newValue);
                    builder.WithTitle(_message.Embeds.First().Title)
                        .WithDescription(description)
                        .WithColor(Color.Orange);
                    properties.Embed = builder.Build();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            
        }
    }
}