using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBotMusic.Modules;
using Victoria;
using Victoria.Entities;
using SearchResult = Discord.Commands.SearchResult;

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
            return Task.CompletedTask;
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
        }

        public async Task MuteAsync(SocketUserMessage message)
        {
            await _player.SetVolumeAsync(0);
            _message = message;
        }

        public async Task UnmuteAsync(SocketUserMessage message)
        {
            await _player.SetVolumeAsync(100);   
            _message = message;
        }
        public async Task SkipAsync(SocketUserMessage message)
        {
            if (_player is null || _player.Queue.Count is 0)
            {
                //await _player.TextChannel.SendMessageAsync("Nothing in queue.");
                return;
            }
            
            LavaTrack oldTrack = _player.CurrentTrack;
            await _player.SkipAsync();
            Embed embed = message.Embeds.First();
                
            await message.ModifyAsync(properties =>
            {
                EmbedBuilder builder = new EmbedBuilder();
                string description = embed.Description.Replace(oldTrack.Title, _player.CurrentTrack.Title);
                builder.WithTitle(embed.Title)
                    .WithDescription(description)
                    .WithColor(Color.Orange);
                properties.Embed = builder.Build();
            });
            //await _player.TextChannel.SendMessageAsync($"Skiped: {oldTrack.Title} \nNow Playing: {_player.CurrentTrack.Title}");
        }

        public async Task<LavaPlayer> TrackListAsync()
        {
            return _player;
        }

        public Task Shuffle()
        {
            _player.Queue.Shuffle();
            return Task.CompletedTask;
        }
        public async Task PauseAsync(SocketUserMessage message)
        {
            if (_player is null) return;
            if (!_player.IsPaused)
            {
                await _player.PauseAsync();
                
                Embed embed = message.Embeds.First();
                string firstString = embed.Description.Substring(0, embed.Description.IndexOf("\n"));
                
                await message.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = embed.Description.Replace(firstString, $"*Status*: **Pausing** `{_player.CurrentTrack.Title}`");
                    builder.WithTitle(embed.Title)
                        .WithDescription(description)
                        .WithColor(Color.Orange);
                    properties.Embed = builder.Build();
                });
            }
            else
            {
                await ResumeAsync(message);
            }
        }

        public async Task ResumeAsync(SocketUserMessage message)
        {
            if (_player is null) return;
            if (_player.IsPaused)
            {
                await _player.ResumeAsync();
                
                Embed embed = message.Embeds.First();
                string firstString = embed.Description.Substring(0, embed.Description.IndexOf("\n"));
                
                await message.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = embed.Description.Replace(firstString, $"*Status*: **Playing** `{_player.CurrentTrack.Title}`");
                    builder.WithTitle(embed.Title)
                        .WithDescription(description)
                        .WithColor(Color.Orange);
                    properties.Embed = builder.Build();
                });
            }
            else
            {
                await PauseAsync(message);
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