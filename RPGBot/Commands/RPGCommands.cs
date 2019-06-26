using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Commands {

    public class RPGCommands : BaseCommandModule {

        [Command("setchannel")]
        public async Task SetChannel(CommandContext ctx, DiscordChannel channel = null) {
            if (channel == null) {
                channel = ctx.Channel;
            }

            if (channel.Type != DSharpPlus.ChannelType.Text) {
                await ctx.RespondAsync("This is not a text channel!");
                return;
            }
            if (!Bot.ConfiguredChannels.ContainsKey(ctx.Guild.Id)) {
                Bot.ConfiguredChannels.TryAdd(ctx.Guild.Id, null);
            }
            if (Bot.ConfiguredChannels.TryUpdate(ctx.Guild.Id, channel, null)) {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")}");
            }
        }

        public static async Task<string> GetURL(string path) {
            var msg = await Bot.ImageCache.SendFileAsync(path);
            File.Delete(path);
            return msg.Attachments.First().Url;
        }

        public static async Task Mission(DiscordGuild guild) {
            //First we try to grab the channel defined, otherwise we pick default channel, and send a message to the owner on how to actually setup the default channel..
            if (!Bot.ConfiguredChannels.TryGetValue(guild.Id, out var channel)) {
                channel = guild.GetDefaultChannel();
                await guild.Owner.SendMessageAsync($"Hey there! Seems like you didn't set a default channel up for RPG-Bot! You can do so by heading into a channel, and typing {Bot.GetPrefix(guild)}setchannel 👌");
            }

            var ev = new PublicEvent(channel);
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
    private const int blockCount = 10;

    public static string GetProcessBar(double percentage) {
        var progressBlockCount = (int)MathF.Round(blockCount * (float)percentage);
        return string.Format("[{0}{1}]", new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount));
    }
}