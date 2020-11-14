using System.Threading.Tasks;

namespace JetBotMusic
{
    class Program
    {
        static async Task Main(string[] args)
            => await new StreamMusicBot().InitializeAsync(args[0]);
    }
}