using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace JetBotMusic.Modules
{
    public class ServerControl : ModuleBase<SocketCommandContext>
    {
        [Command("ChangeName")]
        public async Task ChangeName(IGuildUser user, string name)
        {
            if (user is null)
            {
                await ReplyAsync("This user not found");
                return;
            }

            if (name is null)
            {
                await ReplyAsync("Need input new NickName");
                return;
            }
            
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("Changed user's name")
                .WithDescription($"{user.Nickname} nickname changed to {name}")
                .WithColor(new Color(new Random().Next(255), new Random().Next(255), new Random().Next(255)));
            await ReplyAsync("", false, builder.Build());
            
            await user.ModifyAsync(properties => { properties.Nickname = name; });
        }

        [Command("React")]
        public async Task AddReaction()
        {    
            await Context.Message.AddReactionAsync(new Emoji("▶"));
        }
    }
}