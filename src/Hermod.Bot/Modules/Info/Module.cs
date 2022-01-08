using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;

namespace Hermod.Bot.Modules.Info
{
    public class Module : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<Module> _logger;

        public Module(ILogger<Module> logger)
        {
            _logger = logger;
        }

        [Command("info")]
        [Alias("stats")]
        [Summary("Provides information about the bot")]
        public async Task Info()
        {
            _logger.LogDebug("Info command executed");
            await ReplyAsync(GetBotStats());
        }

        private string GetBotStats()
        {
            return $"{Format.Bold("Info")}\n" +
                "- Developed by TheIAm79#0951\n" +
                "- Github: `https://github.com/theiam79` \n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                $"- Uptime: {GetUptime()}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {GetHeapSize()} MB\n" +
                $"- Guilds: {Context.Client.Guilds.Count}\n" +
                $"- Channels: {Context.Client.Guilds.Sum(g => g.Channels.Count)}\n" +
                $"- Users: {Context.Client.Guilds.Sum(g => g.MemberCount)}";

            static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
            static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.CurrentCulture);
        }
    }
}
