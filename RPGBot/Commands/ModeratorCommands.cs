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
                    Bot.GuildOptions.Add(guild);
                    DB.Upsert(GuildOption.DBName, GuildOption.TableName, guild);
                }
                await ctx.RespondAsync($"New prefix is now `{Bot.GetPrefix(ctx.Guild)}` 👌");
            } else {
                await ctx.RespondAsync("Please contact an administrator to change the prefix for this guild");
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
        public async Task RunEvent(CommandContext ctx, bool onlyHere = false) {
            if (Bot.BotOwnerIds.Contains(ctx.Member.Id)) {
                var channels = Bot.GuildOptions.Select(x => x.GetChannel());
                if (onlyHere) {
                    channels = new List<DiscordChannel>() { ctx.Channel };
                }
                foreach (var channel in channels) {
                    var quest = new QuestEvent(channel);
                    quest.StartQuest();
                }

                await ctx.RespondAsync($"Executing on {string.Join("\n", channels.Select(x=> $"{x.Guild.Name} - {x.Name}"))}");
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