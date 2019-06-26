using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RPGBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RPGBot.Commands {
    public class InfoCommands : BaseCommandModule {
        [Command("MyStats")]
        [Description("Display your stats in this guild.")]
        [Aliases("Stats")]
        public async Task MyStats(CommandContext ctx) {
            var player = Player.GetPlayer(ctx.Guild.Id, ctx.Member.Id);
            var embed = new DiscordEmbedBuilder()
                .WithAuthor(name: ctx.Member.DisplayName, iconUrl: ctx.Member.AvatarUrl)
                .WithTitle($"Stats for {ctx.Member.DisplayName}")
                .WithColor(DiscordColor.Blue)
                .WithDescription(
                $"**Total Kills:** {player.EnemiesKilled.ToString("N0")}\n" +
                $"**Quests Joined:** {player.TotalQuests.ToString("N0")}\n" +
                $"**Quests Won**: {player.SuccessfulQuests.ToString("N0")}\n" +
                $"**Gold**: {player.Gold.ToString("N0")}\n" +
                $"\n__**Classes**__\n" +
                $"XP: {string.Join("\nXP: ", player.Experience)}");
            await ctx.RespondAsync(embed: embed);
        }
    }
}
