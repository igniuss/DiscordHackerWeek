using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RPGBot.Models;

namespace RPGBot.Commands {

    public class ModeratorCommands : BaseCommandModule {

        [Command("prefix")]
        [Description("Set the prefix")]
        public async Task SetPrefix(CommandContext ctx, string prefix) {
            if (Bot.GuildOptions == null) {
                await ctx.RespondAsync("[ERROR] Bot.Prefixes is Empty; Contact an administrator with this error.");
                return;
            }

            if (!string.IsNullOrEmpty(prefix)) {
                var guild = Bot.GuildOptions.Find(x => x.Id == ctx.Guild.Id);
                if (guild == null) { guild = new GuildOption { Id = ctx.Guild.Id }; }

                guild.Prefix = prefix;
                Bot.GuildOptions.Add(guild);
                DB.Upsert(GuildOption.DBName, GuildOption.TableName, guild);
            }
            await ctx.RespondAsync($"New prefix is now `{Bot.GetPrefix(ctx.Guild)}` 👌");
        }

        [Command("ping")]
        [Description("Example ping command")]
        [Aliases("pong")]
        public async Task Ping(CommandContext ctx) {
            await ctx.TriggerTypingAsync();
            var emoji = DiscordEmoji.FromName(ctx.Client, ":ping_pong:");
            await ctx.RespondAsync($"{emoji} Pong! Ping: {ctx.Client.Ping}ms");
        }
    }
}