using System.IO.Pipes;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBotMusic.Services;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace JetBotMusic.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private MusicService _musicService;

        public Music(MusicService musicService)
        {
            _musicService = musicService;
        }
        
        [Command("Join")]
        public async Task Join()
        {
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user is null || user.VoiceChannel is null)
            {
                await ReplyAsync("You need to connect to a voice channel.");
                return;
            }
            else
            {
                await _musicService.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"now connected to {user.VoiceChannel.Name}");
            }
        }

        [Command("Leave")]
        public async Task Leave()
        {
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user is null)
            {
                await ReplyAsync("Please join the channel the bot is in to make it leave.");
            }
            else
            {
                await _musicService.LeaveAsync(user.VoiceChannel);
                await ReplyAsync($"Bot has now left {user.VoiceChannel.Name}");
            }
        }

        [Command("Play")]
        public async Task Play([Remainder]string query)
        {
            var result = await _musicService.PlayAsync(query, Context.Guild.Id);
            await ReplyAsync(result);
        }

        [Command("Stop")]
        public async Task Stop()
        {
            await _musicService.StopAsync();
            await ReplyAsync("Music playBack stopped.");
        }

        [Command("Skip")]
        public async Task Skip()
        {
            await _musicService.SkipAsync();
        }

        [Command("List")]
        public async Task List()
        {
            LavaPlayer player = await _musicService.TrackListAsync();
            if (player is null) return;
            string listMessage = $"Now playing: {player.CurrentTrack.Title}";
            
            var trackList = player.Queue.Items.ToList();

            if (player.Queue.Count > 0)
            {
                listMessage += "\nTrack in queue:";
                for (int i = 0; i < trackList.Count; i++)
                {
                    var track = trackList[i] as LavaTrack;
                    if (track is null) listMessage += "\nTrack empty";
                    //await ReplyAsync(track.Title);
                    else listMessage += "\n" + track.Title;
                }
            }

            await ReplyAsync(listMessage);
        }
    }
}