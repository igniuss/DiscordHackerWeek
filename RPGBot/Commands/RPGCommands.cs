﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RPGBot.Helpers;
using RPGBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Commands {

    public class RPGCommands : BaseCommandModule {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [Command("shop"), Aliases("store")]
        [Description("View items that can be purchased.")]
        public async Task Shop(CommandContext ctx) {
            var embed = new DiscordEmbedBuilder() {
                Title = "Shop",
                Description = Items.Shop.GetShopDescription(),
                Color = DiscordColor.DarkGreen
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("buy")]
        [Description("Buy an item from the shop.")]
        public async Task Buy(CommandContext ctx, string itemName, int quantity = 1) {
            var player = Player.GetPlayer(ctx.Guild.Id, ctx.Member.Id);
            var item = Items.ItemBase.GetAllItems().Where(x => x.Name.Equals(itemName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (item == null) {
                await ctx.RespondAsync(itemName + " is not an item in the shop. Use the ``shop`` command to see available items.");
                return;
            }
            var price = (ulong)quantity * item.Price;
            if (player.Gold < price) {
                await ctx.RespondAsync($"You do not have enough gold to purchase this item.\nItem Cost: {price}. Your gold: {player.Gold}");
                return;
            }
            // player can buy the item, subtract gold and give item
            player.Gold -= price;
            player.LifetimeMercenariesHired += quantity;
            var first = player.Items.FirstOrDefault(x => x.GetType() == item.GetType());
            if (first == null) {
                player.Items.Add(item);
                first = item;
            }
            first.Count += quantity;
            player.Update();
            await ctx.RespondAsync($"{ctx.Member.Mention}, you bought {quantity} {item.Name} for {price} gold.\nYou have {player.Gold} gold remaining.");
            Logger.Info("[{0}] {1} bought {2} {3} for {4}", ctx.Guild.Name, ctx.Member.Username, quantity, item.Name, price);
        }

        [Command("MyStats")]
        [Description("Display your stats in this guild.")]
        [Aliases("Stats")]
        public async Task MyStats(CommandContext ctx, DiscordUser user = null) {
            if (user == null) { user = ctx.User; }
            var player = Player.GetPlayer(ctx.Guild.Id, user.Id);
            var characters = Characters.CharacterBase.Characters;
            var exp = new Dictionary<Characters.CharacterBase, ulong>();
            foreach (var character in characters) {
                exp.Add(character, player.Experience.GetSafe<ulong>(character.Id, 0));
            }
            var embed = new DiscordEmbedBuilder()
                .WithAuthor(name: ctx.Member.DisplayName, iconUrl: ctx.Member.AvatarUrl)
                .WithTitle($"Stats for {ctx.Member.DisplayName}")
                .WithColor(DiscordColor.Blue)
                .WithDescription(
$@"**Total Kills:** {player.EnemiesKilled.ToString("N0")}
**Quests Joined:** {player.TotalQuests.ToString("N0")}
**Quests Completed**: {player.SuccessfulQuests.ToString("N0")}
**Gold**: {player.Gold.ToString("N0")}
**Lifetime Mercenaries Hired**: {player.LifetimeMercenariesHired}

__**Items**__
{string.Join("\n", player.Items.Select(x => $"{x.GetEmoji()} {x.Name} - {x.Count}"))}

__**Classes**__
Experience :
{string.Join("\n", exp.Select(kv => $"{kv.Key.GetEmoji()} Level {Player.CalculateLevel(kv.Value)} - {kv.Value} exp"))}"
 );
            await ctx.RespondAsync(embed: embed);
        }
    }
}