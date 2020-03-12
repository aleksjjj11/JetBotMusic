using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBotMusic.Services;

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
        [Alias("St", "Setv", "Svolume")]
        public async Task SetVolume(ushort volume)
        {
            await _musicService.SetVolumeAsync(volume);
            await Context.Message.DeleteAsync();
        }
        
        [Command("Join")]
        [Alias("J")]
        public async Task Join()
        {
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user is null)
            {
                await ReplyAsync("User not found.");
                return;
            }
            if (user.VoiceChannel is null)
            {
                await ReplyAsync("You need to connect to a voice channel.");
                return;
            }

            Console.WriteLine($"Amount of users: {user.VoiceChannel.Users.Count}");
            await _musicService.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
            await ReplyAsync($"now connected to {user.VoiceChannel.Name}");

        }

        [Command("Leave")]
        [Alias("Lv")]
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
        [Alias("Shuf", "Sh")]
        public async Task Shuffle()
        {
            await _musicService.Shuffle();
            await Context.Message.DeleteAsync();
        }
        
        [Command("Play")]
        [Alias("P", "Pl")]
        public async Task Play([Remainder]string query)
        {
            var result = await _musicService.PlayAsync(query, Context.Guild);
            BuildPlayingMessage(result);
        }

        [Command("PlaySoundCloud")]
        [Alias("PSC", "PlSC", "PlaySC")]
        public async Task PlaySoundCloud([Remainder] string query)
        {
            var result = await _musicService.PlayAsync(query, Context.Guild, "soundcloud");
            BuildPlayingMessage(result);
        }
        [Command("Seek")]
        [Alias("Sk")]
        public async Task Reset(int hours = 0, int minutes = 0, int seconds = 0)
        {
            hours = hours < 0 || hours > 23 ? 0 : hours;
            minutes = minutes < 0 || minutes > 59 ? 0 : minutes;
            seconds = seconds < 0 || seconds > 59 ? 0 : seconds;
            
            await Context.Message.DeleteAsync();
            await _musicService.SeekAsync(0, hours, minutes, seconds);
        }
        [Command("Stop")]
        [Alias("St", "Stp")]
        public async Task Stop()
        {
            await _musicService.StopAsync();
            await ReplyAsync("Music playBack stopped.");
        }

        [Command("Skip")]
        [Alias("S", "Skp")]
        public async Task Skip()
        {
            await _musicService.SkipAsync();
            await Context.Message.DeleteAsync();
        }

        [Command("Pause")]
        [Alias("Ps", "Wait")]
        public async Task Pause()
        {
            await _musicService.PauseAsync();
            await Context.Message.DeleteAsync();
        }

        [Command("Resume")]
        [Alias("R", "Res", "Rsm")]
        public async Task Resume()
        {
            await _musicService.ResumeAsync();
            await Context.Message.DeleteAsync();
        }
        
        [Command("List")]
        [Alias("L", "Lst")]
        public async Task List()
        {
            await _musicService.TrackListAsync();
            await Context.Message.DeleteAsync();
        }

        [Command("Move")]
        [Alias("M", "Mv")]
        public async Task Move(int numberTrack, int newPosition = 0)
        {
            await _musicService.MoveAsync(numberTrack, newPosition);
            await Context.Message.DeleteAsync();
        }

        [Command("Lyrics")]
        [Alias("Lyr", "Lr", "Lrc")]
        public async Task Lyrics([Remainder] string query = null)
        {
            await Context.Message.DeleteAsync();
            await _musicService.GetLyricsAsync(Context.User, query);
        }

        [Command("Remove")]
        [Alias("Delete", "Del", "D", "Rem", "Rmv")]
        public async Task RemoveaAsync(int index = 0)
        {
            await Context.Message.DeleteAsync();
            await _musicService.RemoveAsync(index);
        }

        [Command("Aliases")]
        [Alias("Help", "Command", "Com", "A")]
        public async Task AliasesAsync()
        {
            await Context.Message.DeleteAsync();
            await _musicService.AliasAsync(Context.User);
            //todo Описать все команды и сделать и вывод по ввду данной команды
        
        }

        [Command("Ping")]
        public async Task PingAsync()
        {
            //todo Выводить задержку с серверами дискорда
            await Context.Channel.SendMessageAsync(StreamMusicBot.Latency.ToString());
        }

        [Command("Loopqueue")]
        [Alias("LoopQ", "LQ")]
        public async Task LoopQueueAsync()
        {
            //todo Реализация должна зацикливать текущую очередь, если в очереди нет песен, то зациклить только эту песню, 
            //через другую команду Loop
            await Context.Message.DeleteAsync();
            
        }

        [Command("Loop")]
        [Alias("Lp")]
        public async Task LoopAsync()
        {
            //todo Зацикливать текущую песню ready
            //todo Добавить состояние зацикливания в меню бота
            bool res = await _musicService.LoopTrackAsync();
            await Context.Message.DeleteAsync();
            var dmChannel = Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.Result.SendMessageAsync(res.ToString());
        }

        [Command("Replay")]
        [Alias("Rep", "Re", "Repl")]
        public async Task ReplayAsync()
        {
            await Context.Message.DeleteAsync();
            await _musicService.ReplayAsync();
        }

        [Command("RemoveDupes")]
        [Alias("RemoveD", "RemDup", "RD", "RDup")]
        public async Task RemoveDupesAsync()
        {
            await Context.Message.DeleteAsync();
            await _musicService.RemoveDupesAsync();
        }

        [Command("LeaveCleanUp")]
        [Alias("LeaveCU", "LCU", "LClean", "Clean", "C")]
        public async Task LeaveCleanupAsync()
        {
            //todo Должно удалять все песни пользователей из очереди, которые не находятся в голосовом чате с ботом
            await Context.Message.DeleteAsync();
            await _musicService.LeaveCleanUpAsync();
        }

        private async void BuildPlayingMessage(string nameSong)
        {
            if (nameSong.Contains("has been added to the queue"))
            {
                await Context.Message.DeleteAsync();
                await _musicService.TrackListAsync();
                return;
            }
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("JetBot-Music")
                .WithDescription($"*Status*: {nameSong}\n" + "*Voice Status*: **Without mute**\n**This time:**`00:00/00:00`🆒\n" +
                                 $"*Ping:* `{StreamMusicBot.Latency}`🛰\n" +
                                 $"***Need votes for skip:*** `1`⏭\n" +
                                 $"🎶**Track in queue:**\n***Nothing***")
                .WithColor(Color.Orange);
            var message = await ReplyAsync("", false, builder.Build());
            
            await message.AddReactionAsync(new Emoji("🚪")); //leave to voice channel (not added)
            await message.AddReactionAsync(new Emoji("⏹")); //stop (not added)
            await message.AddReactionAsync(new Emoji("⏯")); //pause and resume
            await message.AddReactionAsync(new Emoji("⏭")); //skip
            await message.AddReactionAsync(new Emoji("🔀")); //shuffle
            await message.AddReactionAsync(new Emoji("🎼")); //lyrics
            await message.AddReactionAsync(new Emoji("🚫")); //mute and unmute
            
            _musicService.SetMessage(message);
        }
    }
}