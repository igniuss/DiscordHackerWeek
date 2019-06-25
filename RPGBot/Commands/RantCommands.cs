using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RPGBot.Generative;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPGBot.Commands {
    public class RantCommands : BaseCommandModule {
        public DiscordChannel ImageCache;
        [Command("rquest")]
        public async Task GetRandomQuest(CommandContext ctx, int count = 10) {
            try {

                await ctx.RespondAsync($"```{string.Join("\n", QuestGenerator.Instance.GetResults((uint)count))}```");
            } catch (System.Exception ex) {
                await ctx.RespondAsync(ex.ToString());
            }
        }
        [Command("rname")]
        public async Task GetRandomName(CommandContext ctx, int count = 10) {
            await ctx.RespondAsync($"```{string.Join("\n", NameGenerator.Instance.GetResults((uint)count))}```");
        }

        private async Task CheckImageCache(CommandContext ctx) {
            if (this.ImageCache == null) {
                var guild = await ctx.Client.GetGuildAsync(450534004890796042);
                this.ImageCache = guild.GetChannel(593187077609226270);
            }
        }

        [Command("simul")]
        public async Task SimulateQuest(CommandContext ctx) {
            await CheckImageCache(ctx);
            await ctx.Message.DeleteAsync();
            var imgGen = new ImageGenerator();
            var questGen = new QuestGenerator();

            var quest = questGen.GetResult();
            var background = imgGen.RandomBackground();
            var imgPath = imgGen.CreateImage(null, background);

            var imgUpload = await this.ImageCache.SendFileAsync(imgPath);
            var url = imgUpload.Attachments.First().Url;

            File.Delete(imgPath);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Adventure Awaits!")
                .WithImageUrl(url)
                .WithDescription($"Current Quest: {quest}")
                .WithFooter("RPG-Bot - (☞ﾟヮﾟ)☞ Iggy&Jeff Co. ☜(ﾟヮﾟ☜)")
                .WithColor(DiscordColor.CornflowerBlue);


            var msg = await ctx.RespondAsync(embed: embed);
            var emojis = new DiscordEmoji[] {
                DiscordEmoji.FromName(ctx.Client, ":crossed_swords:"),
                DiscordEmoji.FromName(ctx.Client, ":shield:"),
                DiscordEmoji.FromName(ctx.Client, ":ambulance:"),
                DiscordEmoji.FromName(ctx.Client, ":dagger:"),
            };
            foreach (var emoji in emojis) {
                await msg.CreateReactionAsync(emoji);
            }

            await Task.Delay((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
            var votedUsers = new List<ulong>();
            var reactions = new Dictionary<DiscordEmoji, List<DiscordUser>>();

            foreach (var emoji in emojis) {
                var users = await msg.GetReactionsAsync(emoji);
                reactions.Add(emoji, new List<DiscordUser>());
                foreach (var user in users) {
                    if (user.IsBot || votedUsers.Contains(user.Id)) { continue; }
                    reactions[emoji].Add(user);
                    votedUsers.Add(user.Id);
                }
            }

            var builder = new StringBuilder();
            builder.Append("There are a total of ");
            foreach (var key in reactions.Keys) {
                builder.Append($", {reactions[key].Count} {key}s");
            }

            var temp = await ctx.RespondAsync(builder.ToString());
            await Task.Delay((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
            await temp.DeleteAsync("temp message");

            var character = imgGen.RandomCharacter();
            imgPath = imgGen.CreateImage(character, background);

            imgUpload = await this.ImageCache.SendFileAsync(imgPath);
            url = imgUpload.Attachments.First().Url;

            embed.WithTitle("Encountered an Enemy!");
            embed.WithImageUrl(url);
            embed.WithColor(DiscordColor.Red);

            msg = await ctx.RespondAsync(embed: embed);
            await msg.DeleteAllReactionsAsync("rpgbot-stage-reset");
        }
        [Command("genquest")]
        public async Task GenerateQuest(CommandContext ctx) {
            await CheckImageCache(ctx);
            await ctx.Message.DeleteAsync();
            var imgGen = new ImageGenerator();
            var questGen = new QuestGenerator();

            var quest = questGen.GetResult();
            var imgPath = imgGen.RandomFromSearch();

            var imgUpload = await this.ImageCache.SendFileAsync(imgPath);
            var url = imgUpload.Attachments.First().Url;


            var embed = new DiscordEmbedBuilder()
                .WithTitle("Adventure Awaits!")
                .WithImageUrl(url)
                .WithDescription(quest)
                .WithFooter("RPG-Bot - Iggy&Jeff inc ☜(ﾟヮﾟ☜)")
                .WithColor(DiscordColor.CornflowerBlue);
            await ctx.RespondAsync(embed: embed);

        }

        [Command("dorant")]
        public async Task DoRant(CommandContext ctx, [RemainingText] string cmd) {
            try {
                var rant = new Rant.RantEngine();
                rant.LoadPackage("Rantionary.rantpkg");

                var prog = Rant.RantProgram.CompileString(cmd);
                var output = rant.Do(prog);
                await ctx.RespondAsync($"```{string.Join("\n", output.Select(x => x.Value))}```");
            } catch (System.Exception ex) {
                await ctx.RespondAsync(ex.ToString());
            }
        }
    }
}
