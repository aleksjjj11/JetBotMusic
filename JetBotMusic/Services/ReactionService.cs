using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace JetBotMusic.Services
{
    public class ReactionService : ModuleBase<SocketCommandContext>
    {
        private DiscordSocketClient _client;
        private readonly CommandService _cmdService;
        private readonly IServiceProvider _services;
        private readonly MusicService _musicService;
        private string _listReactions;
        private List<SocketGuildUser> _skippingUsers;
        
        public ReactionService(DiscordSocketClient client, CommandService cmdService, IServiceProvider services, MusicService musicService)
        {
            _client = client;
            _cmdService = cmdService;
            _services = services;
            _musicService = musicService;
        }
        public async Task InitializeAsync()
        {
            await _cmdService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.ReactionAdded += ReactionAdded;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel socketMessageChannel, SocketReaction reaction)
        {
            IGuild guild = null;
            foreach (var element in _client.Guilds)
            {
                if (element.GetChannel(reaction.Message.Value.Channel.Id) != null)
                {
                    guild = element;
                    break;
                }
            }
            //⏯    ▶    ⏩    🔊    🚫    🔈    🔀
            if (reaction.User.Value.IsBot) return;
            
            //await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            
            if (reaction.Emote.Name is "⏯")
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                await _musicService.PauseAsync(guild);
                
            }
            
            if (reaction.Emote.Name is "⏭")
            {
                var user = reaction.User.Value as SocketGuildUser;
                var bot = reaction.Message.Value.Author as SocketGuildUser;
                if (!(user.VoiceChannel is null))
                    if (user.VoiceChannel != bot.VoiceChannel)
                    {
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        return;
                    }
                
                if (VoteForSkip(bot.VoiceChannel) <=
                    reaction.Message.Value.Reactions[reaction.Emote].ReactionCount)
                {
                    await _musicService.SkipAsync(guild);
                    var list = reaction.Message.Value.GetReactionUsersAsync(reaction.Emote, 20).FlattenAsync().Result;
                    foreach (var skipUser in list)
                    {
                        if (!skipUser.IsBot)
                            await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, skipUser);
                    }
                }
            }
            
            if (reaction.Emote.Name is "🔊")
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.Message.Value.Author);
                await reaction.Message.Value.AddReactionAsync(new Emoji("🚫"));
                
                await _musicService.UnmuteAsync(guild);
                
                Embed embed = reaction.Message.Value.Embeds.First();
                
                await reaction.Message.Value.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = embed.Description.Replace("*Voice Status*: **With mute**", "*Voice Status*: **Without mute**");
                    builder.WithTitle(embed.Title)
                        .WithDescription(description)
                        .WithColor(Color.Orange);
                    properties.Embed = builder.Build();
                });
            }
            
            if (reaction.Emote.Name is "🔈")
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                await _musicService.DownVolumeAsync(Context.Guild);
            }
            
            if (reaction.Emote.Name is "🚫")
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.Message.Value.Author);
                await reaction.Message.Value.AddReactionAsync(new Emoji("🔊"));
                
                await _musicService.MuteAsync(guild);

                Embed embed = reaction.Message.Value.Embeds.First();
                
                await reaction.Message.Value.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = embed.Description.Replace("*Voice Status*: **Without mute**", "*Voice Status*: **With mute**");
                    builder.WithTitle(embed.Title)
                        .WithDescription(description)
                        .WithColor(Color.Orange);
                    properties.Embed = builder.Build();
                });
            }
            
            if (reaction.Emote.Name is "🔀")
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                await _musicService.Shuffle(guild);
            }

            if (reaction.Emote.Name is "⏹")
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                await _musicService.StopAsync(guild);
                
                Embed embed = reaction.Message.Value.Embeds.First();
                string firstString = embed.Description.Substring(0, embed.Description.IndexOf("\n"));
                
                await reaction.Message.Value.ModifyAsync(properties =>
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    string description = embed.Description.Replace(firstString, "*Status*: **Stopping**");
                    builder.WithTitle(embed.Title)
                        .WithDescription(description)
                        .WithColor(Color.Orange);
                    properties.Embed = builder.Build();
                });
            }

            if (reaction.Emote.Name is "🚪")
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                if (!((reaction.User.Value as SocketGuildUser).VoiceChannel is null))
                    await _musicService.LeaveAsync((reaction.User.Value as SocketGuildUser).VoiceChannel);
            }

            if (reaction.Emote.Name is "🎼")
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                await _musicService.GetLyricsAsync(reaction.User.Value as SocketUser, guild);
            }
        }

        public Task SetReactions(string listReactions)
        {
            _listReactions = listReactions;
            return Task.CompletedTask;
        }
        public async Task ReAddedReactions(SocketReaction reaction)
        {
            await reaction.Message.Value.RemoveAllReactionsAsync();
            for (int i = 0; i < _listReactions.Length; i++)
            {
                await reaction.Message.Value.AddReactionAsync(new Emoji("" + _listReactions[i]));
            }
        }

        private int VoteForSkip(SocketVoiceChannel voiceChannel)
        {
            return voiceChannel.Users.Where(user => user.IsDeafened == false).ToList().Count / 2 + 1;
        }
    }
}