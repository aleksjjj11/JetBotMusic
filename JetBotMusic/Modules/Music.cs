using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBotMusic.Services;
using Victoria;

namespace JetBotMusic.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private readonly MusicService _musicService;

        public Music(MusicService musicService)
        {
            _musicService = musicService;
        }

        [Command("SetVolume")]
        [Alias("St", "Setv", "Svolume")]
        public async Task SetVolume(ushort volume)
        {
            await _musicService.SetVolumeAsync(volume, Context.Guild);
            await Context.Message.DeleteAsync();
        }
        
        [Command("Join")]
        [Alias("J")]
        public async Task Join()
        {
            var user = Context.User as SocketGuildUser;

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

            await _musicService.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);

            if (user.VoiceChannel.Id == 693885456663838850)
            {
                await ReplyAsync($"Disappeared");
            }
            else
            {
                await ReplyAsync($"now connected to {user.VoiceChannel.Name}");
            }
        }

        [Command("Leave")]
        [Alias("Lv")]
        public async Task Leave()
        {
            var user = Context.User as SocketGuildUser;

            if (user is null)
            {
                await ReplyAsync("Please join the channel the bot is in to make it leave.");
            }
            else
            {
                await _musicService.LeaveAsync(user.VoiceChannel);
            }
        }

        [Command("Shuffle")]
        [Alias("Shuf", "Sh")]
        public async Task Shuffle()
        {
            await _musicService.Shuffle(Context.Guild);
            await Context.Message.DeleteAsync();
        }
        
        [Command("Play")]
        [Alias("P", "Pl")]
        public async Task Play([Remainder]string query)
        {
            var result = await _musicService.PlayAsync(query, Context.Guild, "youtube", (Context.User as SocketGuildUser)?.VoiceChannel, Context.Channel as ITextChannel);

            if (result.Key is null)
            {
                Console.WriteLine("Result is empty");
                return;
            }

            BuildPlayingMessage(result.Key, result.Value).Wait();
        }

        [Command("PlaySoundCloud")]
        [Alias("PSC", "PlSC", "PlaySC")]
        public async Task PlaySoundCloud([Remainder] string query)
        {
            var result = await _musicService.PlayAsync(query, Context.Guild, "soundcloud", (Context.User as SocketGuildUser)?.VoiceChannel, Context.Channel as ITextChannel);

            if (result.Key is null)
            {
                Console.WriteLine("Result is empty");
                return;
            }

            BuildPlayingMessage(result.Key, result.Value).Wait();
        }

        [Command("Seek")]
        [Alias("Sk")]
        public async Task Reset(int hours = 0, int minutes = 0, int seconds = 0)
        {
            hours = hours < 0 || hours > 23 ? 0 : hours;
            minutes = minutes < 0 || minutes > 59 ? 0 : minutes;
            seconds = seconds < 0 || seconds > 59 ? 0 : seconds;
            
            await Context.Message.DeleteAsync();
            await _musicService.SeekAsync(0, hours, minutes, seconds, Context.Guild);
        }

        [Command("Stop")]
        [Alias("St", "Stp")]
        public async Task Stop()
        {
            await _musicService.StopAsync(Context.Guild);
            await ReplyAsync("Music playBack stopped.");
        }

        [Command("Skip")]
        [Alias("S", "Skp")]
        public async Task Skip()
        {
            await _musicService.SkipAsync(Context.Guild);
            await Context.Message.DeleteAsync();
        }

        [Command("Pause")]
        [Alias("Ps", "Wait")]
        public async Task Pause()
        {
            await _musicService.PauseAsync(Context.Guild);
            await Context.Message.DeleteAsync();
        }

        [Command("Resume")]
        [Alias("R", "Res", "Rsm")]
        public async Task Resume()
        {
            await _musicService.ResumeAsync(Context.Guild);
            await Context.Message.DeleteAsync();
        }
        
        [Command("List")]
        [Alias("L", "Lst")]
        public async Task List()
        {
            //await _musicService.TrackListAsync();
            await Context.Message.DeleteAsync();
        }

        [Command("Move")]
        [Alias("M", "Mv")]
        public async Task Move(int numberTrack, int newPosition = 0)
        {
            await _musicService.MoveAsync(numberTrack, newPosition, Context.Guild);
            await Context.Message.DeleteAsync();
        }

        [Command("Lyrics")]
        [Alias("Lyr", "Lr", "Lrc")]
        public async Task Lyrics([Remainder] string query = null)
        {
            await Context.Message.DeleteAsync();
            await _musicService.GetLyricsAsync(Context.User, Context.Guild ,query);
        }

        [Command("Remove")]
        [Alias("Delete", "Del", "D", "Rem", "Rmv")]
        public async Task RemoveAsync(int index = 0)
        {
            await Context.Message.DeleteAsync();
            await _musicService.RemoveAsync(index, Context.Guild);
        }

        [Command("Aliases")]
        [Alias("Help", "Command", "Com", "A")]
        public async Task AliasesAsync()
        {
            await Context.Message.DeleteAsync();
            await _musicService.AliasAsync(Context.User);
        }

        [Command("Ping")]
        public async Task PingAsync()
        {
            await Context.Channel.SendMessageAsync(StreamMusicBot.Latency.ToString());
        }

        [Command("Loopqueue")]
        [Alias("LoopQ", "LQ")]
        public async Task LoopQueueAsync()
        {
            //todo Реализация должна зацикливать текущую очередь, если в очереди нет песен, то зациклить только эту песню, 
            //через другую команду Loop
            //await Context.Message.DeleteAsync();
            
        }

        [Command("Loop")]
        [Alias("Lp")]
        public async Task LoopAsync()
        {
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
            await _musicService.ReplayAsync(Context.Guild);
        }

        [Command("RemoveDupes")]
        [Alias("RemoveD", "RemDup", "RD", "RDup")]
        public async Task RemoveDupesAsync()
        {
            await Context.Message.DeleteAsync();
            await _musicService.RemoveDupesAsync(Context.Guild);
        }

        [Command("LeaveCleanUp")]
        [Alias("LeaveCU", "LCU", "LClean", "Clean", "C")]
        public async Task LeaveCleanupAsync()
        {
            //todo Должно удалять все песни пользователей из очереди, которые не находятся в голосовом чате с ботом
            await Context.Message.DeleteAsync();
            await _musicService.LeaveCleanUpAsync();
        }

        [Command("YandexPlaylist")]
        [Alias("YP", "YPlaylist")]
        public async Task YandexPlaylist(string url, int startId = 0)
        {
            if (Regex.IsMatch(url, "https://music\\.yandex\\.ru/users/.+/playlists/\\d+") == false)
            {
                Console.WriteLine("Have not matches");
                return;
            }

            string YandexUserName = Regex.Matches(url, "https://music\\.yandex\\.ru/users/(.+)/playlists/(\\d+)").First().Groups[1].Value;
            string YandexPlaylistId = Regex.Matches(url, "https://music\\.yandex\\.ru/users/(.+)/playlists/(\\d+)").First().Groups[2].Value;

            url = $"https://music.yandex.ru/handlers/playlist.jsx?owner={YandexUserName}&kinds={YandexPlaylistId}";
            var result = await _musicService.YandexPlaylistAsync(url, Context.Guild, startId);

            if (result is null)
            {
                Console.WriteLine("Result is empty");
                return;
            }

            for (int i = startId; i < result.Count && i < startId + 10; i++)
            {
                string query = $"{result[i].artists.First().name} - {result[i].title}";
                PlaySoundCloud(query).Wait();
            }
        }

        [Command("YandexAlbum")]
        [Alias("YA", "YAlbum")]
        public async Task YandexAlbum(string url, int startId = 0)
        {
            if (Regex.IsMatch(url, "https://music\\.yandex\\.ru/album/\\d+") == false)
            {
                Console.WriteLine("Bad url");
                return;
            }

            string albumid = Regex.Matches(url, "https://music\\.yandex\\.ru/album/(\\d+)").First().Groups[1].Value;

            url = $"https://music.yandex.ru/handlers/album.jsx?album={albumid}";
            var result = await _musicService.YandexAlbumAsync(url, Context.Guild, startId);

            if (result is null)
            {
                Console.WriteLine("Result is empty");
                return;
            }

            var firstVolume = result.volumes[0];

            Console.WriteLine($"Amount of tracks: {firstVolume.Count}");

            for (int i = startId; i < firstVolume.Count && i < startId + 10; i++)
            {
                string query = $"{firstVolume[i].artists.First().name} - {firstVolume[i].title}";
                PlaySoundCloud(query).Wait();
            }
        }

        [Command("YandexTrack")]
        [Alias("YT", "YTrack")]
        public async Task YandexTrack(string url)
        {
            if (Regex.IsMatch(url, "https://music\\.yandex\\.ru/album/\\d+/track/\\d+") == false)
            {
                Console.WriteLine("Bad url");
                return;
            }

            string trackId = Regex.Matches(url, "https://music\\.yandex\\.ru/album/\\d+/track/(\\d+)").First().Groups[1].Value;
            string query = _musicService.YandexTrackAsync(trackId, Context.Guild).Result;

            await PlaySoundCloud(query);
        }
        
        [Command("Statistic")]
        [Alias("Stat")]
        public async Task StatisticAsync()
        {
            try
            {
                var builder = new EmbedBuilder();

                builder.WithColor(Color.Purple)
                       .WithAuthor("JetBot_Music")
                       .ThumbnailUrl = "https://cdn.discordapp.com/avatars/509581704780840961/72ae1df0ad2955267c783bcf1b2ab52f.png";

                int countTextChannel = 0, countTotalMember = 0;

                foreach (var el in Context.Client.Guilds)
                {
                    countTextChannel += el.TextChannels.Count;
                    countTotalMember += el.MemberCount;
                }

                builder.Fields
                       .Add(new EmbedFieldBuilder()
                       .WithName("Server Count")
                       .WithValue(Context.Client.Guilds.Count).WithIsInline(true));

                builder.Fields
                       .Add(new EmbedFieldBuilder()
                       .WithName("Text Channels")
                       .WithValue(countTextChannel)
                       .WithIsInline(true));

                builder.Fields
                       .Add(new EmbedFieldBuilder()
                       .WithName("Total Members")
                       .WithValue(countTotalMember)
                       .WithIsInline(true));

                builder.Fields
                       .Add(new EmbedFieldBuilder()
                       .WithName("Playing Servers")
                       .WithValue(_musicService.GetCountPlayers).WithIsInline(true));

                var dmChannel = Context.User.GetOrCreateDMChannelAsync();
                await dmChannel.Result.SendMessageAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        private async Task BuildPlayingMessage(LavaTrack track, bool isPlaing)
        {
            string nameSong = track.Title;
            Console.WriteLine($"-----------------<><><><><><>><><<><><><><><>><><> {track.Url}");

            if (isPlaing)
            {
                await Context.Message.DeleteAsync();
                await _musicService.TrackListAsync(Context.Guild);
                return;
            }

            var builder = new EmbedBuilder();

            var description = $"*Status*: **Playing** `{nameSong}`\n" +
                              "*Voice Status*: **Without mute**\n**This time:**`00:00/00:00`🆒\n" +
                              $"*Ping:* `{StreamMusicBot.Latency}`🛰\n" +
                              $"***Need votes for skip:*** `1`⏭\n" +
                              $"🎶**Track in queue:**\n***Nothing***";

            builder.WithTitle("JetBot-Music")
                .WithDescription(description)
                .WithColor(Color.Orange);

            var message = await ReplyAsync("", false, builder.Build());

            _musicService.SetMessage(message);

            await message.AddReactionAsync(new Emoji("🚪")); //leave to voice channel (not added)
            await message.AddReactionAsync(new Emoji("⏹")); //stop (not added)
            await message.AddReactionAsync(new Emoji("⏯")); //pause and resume
            await message.AddReactionAsync(new Emoji("⏭")); //skip
            await message.AddReactionAsync(new Emoji("🔀")); //shuffle
            await message.AddReactionAsync(new Emoji("🎼")); //lyrics
            await message.AddReactionAsync(new Emoji("🚫")); //mute and unmute
        }
    }
}