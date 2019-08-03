using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBotMusic.Services;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace JetBotMusic
{
    public class StreamMusicBot
    {
        private DiscordSocketClient _client;
        private CommandService _cmdService;
        private IServiceProvider _services;
        public StreamMusicBot(DiscordSocketClient client = null, CommandService cmdService = null)
        {
            _client = client ?? new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50,
                LogLevel = LogSeverity.Debug
            });
            _cmdService = cmdService ?? new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = false
            });
        }

        public async Task InitializeAsync()
        {
            await _client.LoginAsync(TokenType.Bot, "NTA5NTgxNzA0NzgwODQwOTYx.XUVzVA.Jd-T0I2DL44rRtxfh3e-YEUsypk");
            await _client.StartAsync();
            _client.Log += LogAsync;
            _services = SetupServices();
            
            var cmdHandler = new CommandHandler(_client, _cmdService, _services);
            await cmdHandler.InitializeAsync();

            await _services.GetRequiredService<MusicService>().InitializeAsync();
            
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage logMessage)
        {
            Console.WriteLine(logMessage.Message);
            return Task.CompletedTask;
        }

        private IServiceProvider SetupServices()
            => new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_cmdService)
                .AddSingleton<CommandHandler>()
                .AddSingleton<MusicService>()
                .AddSingleton<LavaRestClient>()
                .AddSingleton<LavaSocketClient>()
                .BuildServiceProvider();
    }
}