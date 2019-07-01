using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RPGBot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Commands {

    public class ModeratorCommands : BaseCommandModule {

        [Command("prefix")]
        [Description("Set the prefix")]
        public async Task SetPrefix(CommandContext ctx, string prefix) {
            // allow admins or bot owners to change the prefix
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator) || Bot.BotOwnerIds.Contains(ctx.Member.Id)) {
                if (Bot.GuildOptions == null) {
                    await ctx.RespondAsync("[ERROR] Bot.Prefixes is Empty; Contact an administrator with this error.");
                    return;
                }

                if (!string.IsNullOrEmpty(prefix)) {
                    var guild = Bot.GuildOptions.Find(x => x.Id == ctx.Guild.Id);
                    if (guild == null) { guild = new GuildOption { Id = ctx.Guild.Id }; }

                    guild.Prefix = prefix;
                    DB.Upsert(GuildOption.DBName, GuildOption.TableName, guild);
                    Bot.UpdateGuildOptions();
                }
                await ctx.RespondAsync($"New prefix is now `{Bot.GetPrefix(ctx.Guild)}` 👌");
            } else {
                await ctx.RespondAsync("Please contact an administrator to change the prefix for this guild");
            }
        }

        [Command("role")]
        [Description("Set the role to on events")]
        public async Task SetRole(CommandContext ctx, DiscordRole role) {
            // allow admins or bot owners to change the prefix
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator) || Bot.BotOwnerIds.Contains(ctx.Member.Id)) {
                if (Bot.GuildOptions == null) {
                    await ctx.RespondAsync("[ERROR] Bot.Prefixes is Empty; Contact an administrator with this error.");
                    return;
                }
                if (role != null) {
                    if (!role.IsMentionable) {
                        await ctx.RespondAsync($"@{role.Name} isn't mentionable. Please make sure I can mention the role.");
                        return;
                    }

                    var guild = Bot.GuildOptions.Find(x => x.Id == ctx.Guild.Id);
                    if (guild == null) { guild = new GuildOption { Id = ctx.Guild.Id }; }

                    guild.RoleId = role.Id;
                    DB.Upsert(GuildOption.DBName, GuildOption.TableName, guild);
                    Bot.UpdateGuildOptions();
                    await ctx.RespondAsync($"New role is now @{role.Name} 👌\nMake sure the bot can ping this role.");
                } else {
                    await ctx.RespondAsync($"Removed role 👌");
                }
            }
        }

        [Command("bot-stats")]
        public async Task GetStats(CommandContext ctx) {
            if (Bot.BotOwnerIds.Contains(ctx.Member.Id)) {
                var guilds = Bot.Client.Guilds.Values;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Currently in {guilds.Count()} guilds.");
                foreach (var guild in guilds) {
                    sb.AppendLine($"> {guild.Name} - {guild.Owner.DisplayName}  [{guild.MemberCount}]");
                }
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Stats")
                    .WithTimestamp(System.DateTime.Now)
                    .WithDescription(sb.ToString());

                await ctx.RespondAsync(embed: embed);
            }
        }

        [Command("event")]
        public async Task RunEvent(CommandContext ctx, int enemyCount = 1, bool onlyHere = true) {
            if (Bot.BotOwnerIds.Contains(ctx.Member.Id)) {
                await ctx.Message.DeleteAsync();
                var channels = Bot.GuildOptions.Select(x => x.GetChannel());
                if (onlyHere) {
                    channels = new List<DiscordChannel>() { ctx.Channel };
                }
                await Bot.StartEvents(channels, enemyCount);
            }
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