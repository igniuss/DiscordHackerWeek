using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RPGBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Commands {

    public class RPGCommands : BaseCommandModule {

        public static async Task<string> GetURL(string path) {
            var msg = await Bot.ImageCache.SendFileAsync(path);
            return msg.Attachments.First().Url;
        }

        public static async Task Mission(DiscordGuild guild) {
            //First we try to grab the channel defined, otherwise we pick default channel, and send a message to the owner on how to actually setup the default channel..
            if (!Bot.ConfiguredChannels.TryGetValue(guild.Id, out var channel)) {
                channel = guild.GetDefaultChannel();
                await guild.Owner.SendMessageAsync($"Hey there! Seems like you didn't set a default channel up for RPG-Bot! You can do so by heading into a channel, and typing {Bot.GetPrefix(guild)}setchannel 👌");
            }

            var ev = new QuestEvent(channel);
            await ev.StartQuest();
            return;
        }

        [Command("event")]
        public async Task RunEvent(CommandContext ctx, bool onlyHere = false) {
            var guilds = ctx.Client.Guilds.Values.ToList();
            if (onlyHere) {
                guilds = new List<DiscordGuild> {
                    ctx.Guild
                };
            }
            foreach (var guild in guilds) {
                _ = Mission(guild);
            }
            await ctx.RespondAsync($"Executing on {string.Join(", ", guilds.Select(x => x.Name))}");
        }
    }
}

public class ProgressBar {
    private const int blockCount = 30;

    public static string GetProcessBar(double percentage) {
        var progressBlockCount = (int)MathF.Round(blockCount * (float)percentage);
        return string.Format("[{0}{1}]", new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount));
    }
}