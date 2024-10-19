using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBotMusic.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Victoria;

namespace JetBotMusic
{
    public class StreamMusicBot : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _cmdService;
        private readonly IServiceProvider _services;
        // private IServiceProvider _services;
        public static int Latency;

        public StreamMusicBot(DiscordSocketClient client, CommandService cmdService, IServiceProvider services)
        {
            var token = Environment.GetEnvironmentVariable("JetBotMusicBotToken");
            _services = services;
            _client = client;

            _cmdService = cmdService;
        }

        public async Task InitializeAsync()
        {
            var token = Environment.GetEnvironmentVariable("JetBotMusicBotToken");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.Log += LogAsync;
            _client.LatencyUpdated += ClientOnLatencyUpdated;
            _client.UserVoiceStateUpdated += ClientOnUserVoiceStateUpdated;

            // _services = SetupServices();
            
            var cmdHandler = new CommandHandler(_client, _cmdService, _services);
            await cmdHandler.InitializeAsync();

            // await _services.GetRequiredService<MusicService>().InitializeAsync();
            // await _services.GetRequiredService<ReactionService>().InitializeAsync();
            
            // await Task.Delay(-1);
        }

        private Task ClientOnUserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            if (arg1.IsBot && arg1.Id == _client.CurrentUser.Id)
            {
                Console.WriteLine($"Bot went in hell\n{arg2.ToString()}\n{arg3.ToString()}");
            }

            return Task.CompletedTask;
        }

        private Task ClientOnLatencyUpdated(int arg1, int arg2)
        {
            Latency = _client.Latency;
            return Task.CompletedTask;
        }

        private Task LogAsync(LogMessage logMessage)
        {
            Console.WriteLine(logMessage.Message);
            return Task.CompletedTask;
        }

        // private IServiceProvider SetupServices()
        // {
        //     var serviceCollection = new ServiceCollection()
        //         .AddSingleton(_client)
        //         .AddSingleton(_cmdService)
        //         .AddSingleton<CommandHandler>()
        //         .AddSingleton<MusicService>()
        //         .AddSingleton<ReactionService>();
        //     
        //     
        // }
        // .AddLavaNode();
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}