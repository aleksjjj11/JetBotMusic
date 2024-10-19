using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBotMusic.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Victoria;

namespace JetBotMusic
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureServices(collection =>
            {
                collection.AddLogging(x =>
                {
                    x.ClearProviders();
                    x.SetMinimumLevel(LogLevel.Trace);
                });
                collection.AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
                    {
                        AlwaysDownloadUsers = true,
                        MessageCacheSize = 50,
                        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers |
                                         GatewayIntents.GuildPresences | GatewayIntents.MessageContent,
                        LogLevel = LogSeverity.Debug
                    }))
                    .AddSingleton(_ => new CommandService(new CommandServiceConfig
                    {
                        LogLevel = LogSeverity.Verbose,
                        CaseSensitiveCommands = false
                    }))
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<MusicService>()
                    .AddSingleton<ReactionService>();

                collection.AddLavaNode();
                collection.AddHostedService<StreamMusicBot>();
            });

            var app = builder.Build();
            await app.RunAsync();

            await app.Services.UseLavaNodeAsync();

            // await new StreamMusicBot().InitializeAsync(args[0]);
        }
    }
}