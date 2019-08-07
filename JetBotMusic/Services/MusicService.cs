using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
                return $"Now Playing: {track.Title}";
            }
        }

        public async Task StopAsync()
        {
            if (_player is null) return;
            await _player.StopAsync();
        }

        public async Task MuteAsync()
        {
            await _player.SetVolumeAsync(0);
        }

        public async Task UnmuteAsync()
        {
            await _player.SetVolumeAsync(100);
        }
        public async Task SkipAsync(SocketUserMessage message)
        {
            if (_player is null || _player.Queue.Count is 0)
            {
                //await _player.TextChannel.SendMessageAsync("Nothing in queue.");
                return;
            }
            
            var oldTrack = _player.CurrentTrack;
            await _player.SkipAsync();
            await message.ModifyAsync(properties =>
                {
                    properties.Content = $"Now Playing: {_player.CurrentTrack.Title}";
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
        public async Task PauseAsync()
        {
            if (_player is null) return;
            if (!_player.IsPaused)
            {
                await _player.PauseAsync();
                //await _player.TextChannel.SendMessageAsync("Player is paused.");
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
                //await _player.TextChannel.SendMessageAsync("Playback resumed.");
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