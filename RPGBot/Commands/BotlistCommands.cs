using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RPGBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Commands {

    public class BotlistCommands : BaseCommandModule {

        [Command("vote")]
        public async Task Vote(CommandContext ctx) {
            if (Bot.BotlistAPI != null) {
                var weekend = await Bot.BotlistAPI.IsWeekendAsync();
                var voted = await Bot.BotlistAPI.HasVoted(ctx.Member.Id);
                var embed = Bot.GetDefaultEmbed();
                if (voted) {
                    var player = Player.GetPlayer(ctx.Guild.Id, ctx.Member.Id);
                    if (player != null) {
                        var plusEmoji = DiscordEmoji.FromName(ctx.Client, ":blue_heart:");
                        var minEmoji = DiscordEmoji.FromName(ctx.Client, ":no_entry_sign:");
                        var maxStreak = 10ul;
                        embed.AddField("__Streak__", $"{string.Join(' ', Enumerable.Repeat(plusEmoji, (int)player.VoteStreak))} {string.Join(' ', Enumerable.Repeat(minEmoji, (int)(maxStreak - player.VoteStreak)))}");

                        //less than 12 hours ago?.. Yeah no, gotta wait pal!
                        if (DateTime.Now - player.LastVoted < TimeSpan.FromHours(12)) {
                            embed.WithDescription($"Thanks for voting!\nYou've already received your gold though, try again in 12 hours 😉");
                            await ctx.RespondAsync(embed: embed);
                            return;
                        }

                        var goldReceived = 500ul;
                        //less than 24 hours ago?.. Up that streak!
                        if (DateTime.Now - player.LastVoted < TimeSpan.FromDays(1)) {
                            player.VoteStreak++;
                        } else {
                            //reset streak 😔
                            player.VoteStreak = 0;
                        }

                        player.LastVoted = DateTime.Now;

                        if (weekend) { goldReceived *= 2ul; }

                        goldReceived *= Math.Min(maxStreak, player.VoteStreak + 1);

                        //Don't use AddGold, since we don't want a character mult to happen on this 😉
                        player.Gold += goldReceived;
                        player.Update();

                        embed.WithDescription($"Thanks for voting!\nYou've received {goldReceived} gold.");
                        await ctx.RespondAsync(embed: embed);
                        return;
                    }
                } else {
                    embed.WithDescription($"Seems like you haven't voted yet!\nYou can vote [here](https://discordbots.org/bot/591408341608038400/vote) for some free gold!");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }
            }
        }
    }
}