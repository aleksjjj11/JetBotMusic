using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Victoria;
using Victoria.Entities;

namespace JetBotMusic.Services
{
    public class MusicService : ModuleBase<SocketCommandContext>
    {
        private LavaRestClient _lavaRestClient;
        private LavaSocketClient _lavaSocketClient;
        private DiscordSocketClient _client;
        private LavaPlayer _player;
        private IUserMessage _message;

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
            //_lavaSocketClient.OnPlayerUpdated += PlayerUpdated;
            return Task.CompletedTask;
        }

        private async Task PlayerUpdated(LavaPlayer player, LavaTrack track, TimeSpan timeSpan)
        {
            
        }

        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, ITextChannel textChannel)
            => await _lavaSocketClient.ConnectAsync(voiceChannel, textChannel);

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel)
            => await _lavaSocketClient.DisconnectAsync(voiceChannel);

        public void SetMessage(IUserMessage message)
        {
            _message = message;
        }
        public async Task<string> PlayAsync(string query, ulong guildId)
        {
            _player = _lavaSocketClient.GetPlayer(guildId);
            var results = await _lavaRestClient.SearchYouTubeAsync(query);
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
                //await _player.TextChannel.SendMessageAsync("Nothing in queue.");
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
                        listMessage += $"\n`{track.Title}`";
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
            _player.CurrentTrack.ResetPosition();
        }

        public async Task ResetPlay()
        {
            
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
        private async Task TrackFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            if (!reason.ShouldPlayNext()) return;
            if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTack))
            {
                await player.TextChannel.SendMessageAsync("There are no more tracks in the queue");
                return;
            }

            string firstString = _message.Embeds.First().Description.Substring(0, _message.Embeds.First().Description.IndexOf("\n"));
                
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