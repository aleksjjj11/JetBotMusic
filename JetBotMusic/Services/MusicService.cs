using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Common;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JavaAnSharp;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Interfaces;
using Victoria.Responses.Rest;
using Yandex.Music.Api;
using Yandex.Music.Api.Models;
using YandexAPI;

namespace JetBotMusic.Services
{
    public class MusicService : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _client;
        private Queue<LavaTrack> _loopQueue;
        private LavaTrack _loopTrack;
        private IUserMessage _messageLyrics = null;
        private List<IUserMessage> _messagesLyrics = null;
        private bool _isLoopTrack = false;
        private Dictionary<IMessageChannel, IUserMessage> GUIMessages;

        public bool IsPlaying(IGuild guild)
        {
            return _lavaNode.GetPlayer(guild).PlayerState == PlayerState.Playing;
        }

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
            _lavaNode.OnWebSocketClosed += LavaNodeOnOnWebSocketClosed;

            GUIMessages = new Dictionary<IMessageChannel, IUserMessage>();

            return Task.CompletedTask;
        }

        private async Task LavaNodeOnOnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            Console.WriteLine("Bot reconnected on voice channel");

            await _lavaNode.JoinAsync(_lavaNode.GetPlayer(_client.GetGuild(arg.GuildId)).VoiceChannel);
        }

        private Task PlayerUpdated(PlayerUpdateEventArgs playerUpdateEventArgs)
        {
            TrackListAsync(playerUpdateEventArgs.Player).Wait();
            UpdateVote(playerUpdateEventArgs.Player).Wait();
            UpdatePing(playerUpdateEventArgs.Player).Wait();
            TimeAsync(playerUpdateEventArgs.Player).Wait();
            UpdateTrack(playerUpdateEventArgs.Player).Wait();

            return Task.CompletedTask;
        }

        private async Task UpdateTrack(LavaPlayer player)
        {
            var message = GUIMessages[player.TextChannel];
            var embed = message.Embeds.First();
            var lengthOldTrackName = embed.Description.IndexOf("\n");
            var firstString = embed.Description.Substring(0, lengthOldTrackName);

            if (firstString.Contains(player.Track.Title) is true) 
                return;

            await message.ModifyAsync(properties =>
            {
                var builder = new EmbedBuilder();

                var newTrackLine = $"*Status*: **{player.PlayerState}** `{player.Track.Title}`";
                var description = embed.Description.Replace(firstString, newTrackLine);

                builder.WithTitle(embed.Title)
                       .WithDescription(description)
                       .WithColor(Color.Orange);

                properties.Embed = builder.Build();
            });
        }

        private async Task UpdatePing(LavaPlayer player)
        {
            var message = GUIMessages[player.TextChannel];

            await message.ModifyAsync(properties =>
            {
                var embed = message.Embeds.First();
                var startIndex = embed.Description.IndexOf("*Ping:*");
                var endIndex = embed.Description.IndexOf("🛰");
                var lengthPingStr = endIndex - startIndex;

                var oldPing = embed.Description.Substring(startIndex, lengthPingStr);
                var newPing = $"*Ping:* `{StreamMusicBot.Latency}`";

                var builder = new EmbedBuilder();

                var embedColor = embed.Color.Value;
                var newTitle = embed.Title;
                var newDescription = embed.Description.Replace(oldPing, newPing);

                builder.WithTitle(newTitle)
                       .WithDescription(newDescription)
                       .WithColor(embedColor);

                properties.Embed = builder.Build();
            });
        }

        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, ITextChannel textChannel)
            => await _lavaNode.JoinAsync(voiceChannel, textChannel);

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel)
            => await _lavaNode.LeaveAsync(voiceChannel);

        private async Task TimeAsync(LavaPlayer player)
        {
            var message = GUIMessages[player.TextChannel];
            var timeSpan = player.Track.Position;
            var track = player.Track;

            await message.ModifyAsync(properties =>
            {
                var embed = message.Embeds.First();
                var startIndex = embed.Description.IndexOf("**This time:**");
                var lengthLine = embed.Description.IndexOf("🆒") - startIndex;
                string oldStr = embed.Description.Substring(startIndex, lengthLine);

                string timeMSG = default;

                if (player.Track.IsStream)
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

                    timeMSG = $"**This time:**`{oldMinutes}:{oldSeconds}/{newMinutes}:{newSeconds}`";
                }

                var builder = new EmbedBuilder();

                builder.WithTitle(embed.Title)
                    .WithDescription(embed.Description.Replace(oldStr, timeMSG))
                    .WithColor(embed.Color.Value);

                properties.Embed = builder.Build();
            });
        }

        public void SetMessage(IUserMessage message)
        {
            try
            {
                GUIMessages.Add(message.Channel, message);
            }
            catch (ArgumentException ex)
            {
                GUIMessages[message.Channel] = message;
            }
        }

        public async Task GetLyricsAsync(SocketUser user, IGuild guild, string query = null)
        {
            LavaPlayer player = _lavaNode.GetPlayer(guild);
            //Массив строк для неопреледелённого количества сообщения для вывода текста песен
            var listLyrics = new List<string>();

            if (player.Track != null)
            {
                query = player.Track.Title;
            }

            var musicApi = new YandexMusicApi();
            var yandexTracks = musicApi.SearchTrack(query);

            if (yandexTracks is null || yandexTracks.Count < 1)
            {
                Console.WriteLine("Yandex DEBUG ---------------------->> NUUUUUULL");
                listLyrics[0] = "**NOT FOUND**";
            }

            var track = yandexTracks?.First();

            if (track is null)
            {
                listLyrics[0] = "**NOT FOUND**";
            }
            else
            {
                var parser = new ParserGenius($"{track.Artists.First().Name} - {track.Title}");
                parser.Initialization().Wait();

                listLyrics = parser.DivideLyrics();
            }

            var dmChannel = user.GetOrCreateDMChannelAsync();

            for (int i = 0; i < listLyrics.Count; i++)
            {
                var builder = new EmbedBuilder();

                builder.WithTitle("Lyrics")
                    .WithDescription(listLyrics[i])
                    .WithColor(Color.Red);

                await dmChannel.Result.SendMessageAsync("", false, builder.Build());
            }
        }

        public async Task<KeyValuePair<LavaTrack, bool>> PlayAsync(string query, SocketGuild guild, string source = "youtube", SocketVoiceChannel voiceChannel = null, ITextChannel textChannel = null)
        {
            if (voiceChannel != null && textChannel != null && _lavaNode.TryGetPlayer(guild, out var player) == false)
            {
                ConnectAsync(voiceChannel, textChannel).Wait();
            }

            player = _lavaNode.GetPlayer(guild);
            SearchResponse res;

            switch (source)
            {
                case "youtube":
                    res = await _lavaNode.SearchYouTubeAsync(query);
                    break;
                case "soundcloud":
                    res = await _lavaNode.SearchSoundCloudAsync(query);
                    break;
                default:
                    res = await _lavaNode.SearchYouTubeAsync(query);
                    break;
            }

            var track = res.Tracks.FirstOrDefault();

            if (track?.Title == "Track empty" || track is null) 
                return new KeyValuePair<LavaTrack, bool>(null, false);

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                player.Queue.Enqueue(track);
                return new KeyValuePair<LavaTrack, bool>(track, true);
            }

            await player.PlayAsync(track);
            await player.UpdateVolumeAsync(100);

            return new KeyValuePair<LavaTrack, bool>(track, false);
        }

        public async Task StopAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (player is null) return;

            _messageLyrics = null;

            await player.StopAsync();
            await TrackListAsync(guild);
        }

        public async Task MuteAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            await player.UpdateVolumeAsync(0);
        }

        public async Task UnmuteAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            await player.UpdateVolumeAsync(100);
        }

        public async Task AliasAsync(SocketUser user)
        {
            var builder = new EmbedBuilder();
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

        public async Task SkipAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            var message = GUIMessages[player.TextChannel];

            if (player.Queue.Count is 0) return;

            var oldTrack = player.Track;

            await player.SkipAsync();

            await message.ModifyAsync(properties =>
            {
                var builder = new EmbedBuilder();
                var embed = message.Embeds.First();
                var description = embed.Description.Replace(oldTrack.Title, player.Track.Title);

                builder.WithTitle(embed.Title)
                       .WithDescription(description)
                       .WithColor(Color.Orange);

                properties.Embed = builder.Build();
            });

            await TrackListAsync(guild);
        }

        public async Task TrackListAsync(LavaPlayer player)
        {
            var message = GUIMessages[player.TextChannel];
            var listMessage = "";
            var embed = message.Embeds.First();
            var startIndex = embed.Description.IndexOf("\n🎶");
            var descriptionLength = embed.Description.Length - startIndex;

            string useless = embed.Description.Substring(startIndex, descriptionLength);

            var trackList = player.Queue.ToList();

            if (player.Queue.Count > 0)
            {
                listMessage += "\n🎶**Track in queue:**";
                for (int i = 0; i < trackList.Count; i++)
                {
                    var track = trackList[i] as LavaTrack;

                    if (track is null)
                    {
                        listMessage += "\n`Track empty`";
                    }
                    else
                    {
                        listMessage += $"\n```css\n .{i} [{track.Title}]```";
                    }
                }
            }
            else
            {
                listMessage = "\n🎶**Track in queue:**\n***Nothing***";
            }

            await message.ModifyAsync(properties =>
            {
                listMessage = embed.Description.Replace(useless, " ") + listMessage;

                var builder = new EmbedBuilder();

                builder.WithTitle(embed.Title)
                       .WithDescription(listMessage)
                       .WithColor(Color.LightOrange);

                properties.Embed = builder.Build();
            });
        }

        public async Task TrackListAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            var message = GUIMessages[player.TextChannel];
            var listMessage = "";
            var embed = message.Embeds.First();
            var startIndex = embed.Description.IndexOf("\n🎶");
            var descriptionLength = embed.Description.Length - startIndex;

            var useless = embed.Description.Substring(startIndex, descriptionLength);

            var trackList = player.Queue.ToList();

            if (player.Queue.Count > 0)
            {
                listMessage += "\n🎶**Track in queue:**";

                for (int i = 0; i < trackList.Count; i++)
                {
                    var track = trackList[i] as LavaTrack;

                    if (track is null)
                    {
                        listMessage += "\n`Track empty`";
                    }
                    else
                    {
                        listMessage += $"\n```css\n .{i} [{track.Title}]```";
                    }
                }
            }
            else
            {
                listMessage = "\n🎶**Track in queue:**\n***Nothing***";
            }

            await message.ModifyAsync(properties =>
            {
                listMessage = embed.Description.Replace(useless, " ") + listMessage;

                var builder = new EmbedBuilder();

                builder.WithTitle(embed.Title)
                       .WithDescription(listMessage)
                       .WithColor(Color.LightOrange);

                properties.Embed = builder.Build();
            });
        }

        public async Task SeekAsync(int days, int hours, int minutes, int second, IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            var time = new TimeSpan(days, hours, minutes, second);

            await player.SeekAsync(time);
            await player.TextChannel.SendMessageAsync();
        }

        public async Task Shuffle(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            player.Queue.Shuffle();

            await TrackListAsync(guild);
        }

        public async Task PauseAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (player is null) return;

            var message = GUIMessages[player.TextChannel];

            if (player.PlayerState != PlayerState.Paused)
            {
                await player.PauseAsync();

                var embed = message.Embeds.First();
                var firstString = embed.Description.Substring(0, embed.Description.IndexOf("\n"));

                await message.ModifyAsync(properties =>
                {
                    var builder = new EmbedBuilder();
                    var description = embed.Description.Replace(firstString, $"*Status*: **Pausing** `{player.Track.Title}`");

                    builder.WithTitle(embed.Title)
                           .WithDescription(description)
                           .WithColor(Color.Orange);

                    properties.Embed = builder.Build();
                });
            }
            else
            {
                await ResumeAsync(guild);
            }
        }

        public async Task ResumeAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (player is null) return;

            var message = GUIMessages[player.TextChannel];

            if (player.PlayerState == PlayerState.Paused)
            {
                await player.ResumeAsync();

                var embed = message.Embeds.First();
                var oldValue = embed.Description.Substring(0, embed.Description.IndexOf("\n"));

                await message.ModifyAsync(properties =>
                {
                    var builder = new EmbedBuilder();
                    var newValue = $"*Status*: **Playing** `{player.Track.Title}`";
                    var description = embed.Description.Replace(oldValue, newValue);

                    builder.WithTitle(embed.Title)
                           .WithDescription(description)
                           .WithColor(Color.Orange);

                    properties.Embed = builder.Build();
                });
            }
            else
            {
                await PauseAsync(guild);
            }
        }

        public async Task SetVolumeAsync(ushort value, IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (value > 150)
            {
                await player.TextChannel.SendMessageAsync("Incorrect value for volume");
                return;
            }

            await player.UpdateVolumeAsync(value);
        }

        public async Task UpVolumeAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (player.Volume + 10 <= 150)
            {
                await player.UpdateVolumeAsync((ushort) (player.Volume + 10));
            }
            else
            {
                await player.UpdateVolumeAsync(150);
            }
        }

        public async Task DownVolumeAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (player.Volume - 10 >= 0)
            {
                await player.UpdateVolumeAsync((ushort) (player.Volume - 10));
            }
            else
            {
                await player.UpdateVolumeAsync(0);
            }
        }

        public async Task MoveAsync(int numberTrack, int newPosition, IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            //Если номер трека совпадает с его позицией, то ничего не делаем
            if (numberTrack == newPosition || player.Queue.Count is 0) return;
            if (numberTrack >= player.Queue.Count || newPosition >= player.Queue.Count) return;
            //Получаем нашу очередь в лист 
            var queue = player.Queue.ToList();

            //Добаляем трек в новую позицию и удаляем со старой
            queue.Insert(newPosition, queue[numberTrack]);

            if (newPosition < numberTrack)
            {
                queue.RemoveAt(numberTrack + 1);
            }
            else
            {
                queue.RemoveAt(numberTrack);
            }
            
            player.Queue.Clear();

            foreach (var element in queue)
            {
                player.Queue.Enqueue(element);
            }

            await TrackListAsync(guild);
        }

        private async Task TrackFinished(TrackEndedEventArgs trackEndedEventArgs)
        {
            var reason = trackEndedEventArgs.Reason;
            var player = trackEndedEventArgs.Player;
            var track = trackEndedEventArgs.Track;
            var message = GUIMessages[player.TextChannel];
            var embed = message.Embeds.First();
            var descriptionLength = embed.Description.IndexOf("\n");
            var firstString = embed.Description.Substring(0, descriptionLength);

            if (reason.ShouldPlayNext() == false) return;

            if (_isLoopTrack)
            {
                await message.ModifyAsync(properties =>
                {
                    var builder = new EmbedBuilder();
                    var newValue = $"*Status*: **Playing** `{track.Title}`";
                    var description = embed.Description.Replace(firstString, newValue);

                    builder.WithTitle(embed.Title)
                           .WithDescription(description)
                           .WithColor(Color.Orange);

                    properties.Embed = builder.Build();
                });

                await player.PlayAsync(track);
                await TrackListAsync(player);

                return;
            }

            if (player.Queue.TryDequeue(out var item) == false || !(item is LavaTrack nextTack))
            {
                await message.ModifyAsync(properties =>
                {
                    var builder = new EmbedBuilder();
                    var newValue = $"*Status*: **Played** `{track.Title}`";
                    var description = embed.Description.Replace(firstString, newValue);

                    builder.WithTitle(embed.Title)
                           .WithDescription(description)
                           .WithColor(Color.LightOrange);

                    properties.Embed = builder.Build();
                });

                _messageLyrics = null;

                return;
            }

            await message.ModifyAsync(properties =>
            {
                var builder = new EmbedBuilder();
                var newValue = $"*Status*: **Playing** `{nextTack.Title}`";
                var description = embed.Description.Replace(firstString, newValue);

                builder.WithTitle(embed.Title)
                       .WithDescription(description)
                       .WithColor(Color.Orange);

                properties.Embed = builder.Build();
            });

            await player.PlayAsync(nextTack);
            await TrackListAsync(player);
        }

        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.Message);
            return Task.CompletedTask;
        }

        public async Task RemoveAsync(int index, IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            int i = 0;

            foreach (var element in player.Queue)
            {
                if (i == index)
                {
                    player.Queue.Remove(element);
                    break;
                }

                i++;
            }

            await TrackListAsync(player);
        }

        public async Task ReplayAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            await player.PlayAsync(player.Track);
        }

        public async Task RemoveDupesAsync(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);

            if (player.Queue.Count is 0) return;

            for (int i = 0; i < player.Queue.Count - 1; i++)
            {
                var firstTrack = player.Queue.GetItemByIndex(i) as LavaTrack;

                for (int j = i + 1; j < player.Queue.Count; j++)
                {
                    var secondTrack = player.Queue.GetItemByIndex(j) as LavaTrack;

                    if (firstTrack?.Id == secondTrack?.Id)
                    {
                        player.Queue.Remove(secondTrack);
                    }
                }
            }

            await TrackListAsync(player);
        }

        public async Task LeaveCleanUpAsync()
        {
            //TODO delete songs of users who left voice channel
        }

        public async Task<bool> LoopTrackAsync()
        {
            _isLoopTrack = !_isLoopTrack;
            return _isLoopTrack;
        }

        public async Task LoopQueueAsync(IGuild guild)
        {
            //_isLoopQueue = !_isLoopQueue;
            //_loopTrack = _player.Track;
        }

        private async Task UpdateVote(LavaPlayer player)
        {
            var message = GUIMessages[player.TextChannel];

            try
            {
                if ((player.VoiceChannel as SocketVoiceChannel) is null)
                {
                    Console.WriteLine($"VOICE CHANNEL IS NULL");
                    return;
                }

                int amountVote = (player.VoiceChannel as SocketVoiceChannel).Users.Count / 2 + 1;
                string newValue = $"***Need votes for skip:*** `{amountVote}`⏭";

                var embed = message.Embeds.First();

                int startIndex = embed.Description.IndexOf("***Need votes for skip:***");
                int endIndex = embed.Description.IndexOf("⏭") + 1;
                string oldValue = embed.Description.Substring(startIndex, endIndex - startIndex);

                Console.WriteLine($"START INDEX: {startIndex} END INDEX: {endIndex}\nNew value:{newValue}\nOld:{oldValue}");

                await message.ModifyAsync(properties =>
                {
                    var builder = new EmbedBuilder();
                    var description = embed.Description.Replace(oldValue, newValue);

                    builder.WithTitle(embed.Title)
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

        public async Task<List<TracksItem>> YandexPlaylistAsync(string url, SocketGuild guild, int startId = 0)
        {
            var client = new HttpClient();
            var response = client.GetAsync(url).Result;

            string textResponse = response.Content.ReadAsStringAsync().Result;
            var root = JsonSerializer.Deserialize<PlaylistRoot>(textResponse);

            if (root.playlist.tracks.Count == 0)
                return null;

            return root.playlist.tracks;
        }

        public async Task<AlbumRoot> YandexAlbumAsync(string url, SocketGuild contextGuild, int startId)
        {
            var client = new HttpClient();
            var response = client.GetAsync(url).Result;

            string textResponse = response.Content.ReadAsStringAsync().Result;
            var root = JsonSerializer.Deserialize<AlbumRoot>(textResponse);

            if (root.volumes[0].Count == 0)
                return null;

            return root;
        }

        public async Task<string> YandexTrackAsync(string trackId, SocketGuild contextGuild)
        {
            var yandexMusicApi = new YandexMusicApi();
            var track = yandexMusicApi.GetTrack(trackId);

            return $"{track.Title} - {track.Artists.First().Name}";
        }

        public long GetCountPlayers => _lavaNode.Players.Count();
    }
}