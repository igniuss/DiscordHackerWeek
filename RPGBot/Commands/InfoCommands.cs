using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RPGBot.Helpers;
using RPGBot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Commands {

    public class InfoCommands : BaseCommandModule {

        [Command("MyStats")]
        [Description("Display your stats in this guild.")]
        [Aliases("Stats")]
        public async Task MyStats(CommandContext ctx) {
            var player = Player.GetPlayer(ctx.Guild.Id, ctx.Member.Id);
            var characters = Characters.CharacterBase.GetAllCharacters();
            var exp = new Dictionary<Characters.CharacterBase, ulong>();
            foreach (var character in characters) {
                exp.Add(character, player.Experience.GetSafe<ulong>(character.Id, 0));
            }

            var embed = new DiscordEmbedBuilder()
                .WithAuthor(name: ctx.Member.DisplayName, iconUrl: ctx.Member.AvatarUrl)
                .WithTitle($"Stats for {ctx.Member.DisplayName}")
                .WithColor(DiscordColor.Blue)
                .WithDescription(
                $"**Total Kills:** {player.EnemiesKilled.ToString("N0")}\n" +
                $"**Quests Joined:** {player.TotalQuests.ToString("N0")}\n" +
                $"**Quests Completed**: {player.SuccessfulQuests.ToString("N0")}\n" +
                $"**Gold**: {player.Gold.ToString("N0")}\n" +

                $"\n__**Classes**__\n" +
                $" Experience : \n{string.Join("\n", exp.Select(kv => $"{kv.Key.GetEmoji()} Level {Player.CalculateLevel(kv.Value)} - {kv.Value} exp"))}");
            await ctx.RespondAsync(embed: embed);
        }
    }
}