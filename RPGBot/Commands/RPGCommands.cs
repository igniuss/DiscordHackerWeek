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
            var foundGuild = Bot.GuildOptions.Find(x => x.Id == guild.Id);
            if (foundGuild != null) {
                var channel = foundGuild.GetChannel();
                var ev = new QuestEvent(channel);
                await ev.StartQuest();
            }
            return;
        }

        [Command("setchannel")]
        public async Task SetChannel(CommandContext ctx, DiscordChannel channel = null) {
            if (channel == null) {
                channel = ctx.Channel;
            }
            if (channel.Type == DSharpPlus.ChannelType.Text) {
                var guild = Bot.GuildOptions.Find(x => x.Id == ctx.Guild.Id);
                if (guild == null) {
                    guild = new GuildOption { Id = ctx.Guild.Id };
                }
                guild.Channel = channel.Id;
                try {
                    DB.Upsert(GuildOption.DBName, GuildOption.TableName, guild);
                    Bot.GuildOptions = DB.GetAll<GuildOption>(GuildOption.DBName, GuildOption.TableName).ToList();
                    await ctx.RespondAsync($"👌 New default channel is now {guild.GetChannel()}");
                } catch (System.Exception ex) {
                    Console.WriteLine(ex);
                }
            }
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