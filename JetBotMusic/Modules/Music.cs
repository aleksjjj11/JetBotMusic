using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBotMusic.Services;
using Victoria.Entities;

namespace JetBotMusic.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private MusicService _musicService;
        private ReactionService _reactionService;
        public Music(MusicService musicService)
        {
            _musicService = musicService;
        }

        [Command("SetVolume")]
        public async Task SetVolume(int volume)
        {
            await _musicService.SetVolumeAsync(volume);
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

        [Command("Shuffle")]
        public async Task Shuffle()
        {
            await _musicService.Shuffle();
        }
        
        [Command("Play")]
        public async Task Play([Remainder]string query)
        {
            var result = await _musicService.PlayAsync(query, Context.Guild.Id);
            if (result.Contains("has been added to the queue"))
            {
                var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(result);
                
                await _musicService.TrackListAsync();
                
                return;
            }
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("JetBot-Music")
                .WithDescription($"*Status*: {result}" + "\n*Voice Status*: **Without mute**\n🎶**Track in queue:**\n***Nothing***")
                .WithColor(Color.Orange);
            var message = await ReplyAsync("", false, builder.Build());
            
            await message.AddReactionAsync(new Emoji("🚪")); //leave to voice channel (not added)
            await message.AddReactionAsync(new Emoji("⏹")); //stop (not added)
            await message.AddReactionAsync(new Emoji("⏯")); //pause and resume
            await message.AddReactionAsync(new Emoji("⏭")); //skip
            await message.AddReactionAsync(new Emoji("🔀")); //shuffle
            await message.AddReactionAsync(new Emoji("🚫")); //mute and unmute
            
            _musicService.SetMessage(message);
        }

        [Command("Reset")]
        public async Task Reset()
        {
            await _musicService.ResetPlay();
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

        [Command("Pause")]
        public async Task Pause()
        {
            await _musicService.PauseAsync();
        }

        [Command("Resume")]
        public async Task Resume()
        {
            await _musicService.ResumeAsync();
        }

        [Command("Lyrics")]
        public async Task Lyrics()
        {
            await _musicService.LyricsAsync();
        }
        [Command("List")]
        public async Task List()
        {
            await _musicService.TrackListAsync();
        }
    }
}