using System.Collections.Generic;
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
        private ReactionService _reactionService;
        private string listEmoji;
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
                listEmoji = "🚫⏯⏭🔀";
                await _reactionService.SetReactions(listEmoji);
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
            
        }
        
        [Command("Play")]
        public async Task Play([Remainder]string query)
        {
            var result = await _musicService.PlayAsync(query, Context.Guild.Id);
            if (result.Contains("has been added to the queue"))
            {
                var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(result);
                return;
            }
            var message = await ReplyAsync(result);
            //List<Emoji> listReaction = null;
             //await ReplyAsync("🔊:mute:⏯⏩");
             /*for (int i = 0; i < listEmoji.Length; i++)
             {
                 await message.AddReactionAsync(new Emoji("" + listEmoji[i]));
             }*/
            //await message.AddReactionAsync(new Emoji("🔊"));
            await message.AddReactionAsync(new Emoji("🚪")); //leave to voice channel (not added)
            await message.AddReactionAsync(new Emoji("⏹")); //stop (not added)
            await message.AddReactionAsync(new Emoji("⏯")); //pause and resume
            await message.AddReactionAsync(new Emoji("⏭")); //skip
            await message.AddReactionAsync(new Emoji("🔀")); //shuffle
            await message.AddReactionAsync(new Emoji("🚫")); //mute and unmute
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
            //await _musicService.SkipAsync();
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
        [Command("List")]
        public async Task List()
        {
            var player = await _musicService.TrackListAsync();
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
            //Отправляем список песен в ЛС пользователю, который его запросил
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(listMessage);
        }
    }
}