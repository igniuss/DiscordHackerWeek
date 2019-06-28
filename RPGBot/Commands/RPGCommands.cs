using DSharpPlus;
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
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator)) {
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
            } else {
                await ctx.RespondAsync("This command can only be used by server admins.");
            }
        }
        [Command("event")]
        public async Task RunEvent(CommandContext ctx, bool onlyHere = false) {
            if (Bot.BotOwnerIds.Contains(ctx.Member.Id)) {
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
            if(first == null) {
                player.Items.Add(item);
                first = item;
            }
            first.Count += quantity;
            player.Update();
            await ctx.RespondAsync($"{ctx.Member.Mention}, you bought {quantity} {item.Name} for {price} gold.\nYou have {player.Gold} gold remaining.");
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