using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace RPGBot.Commands {

    public class HelpCommands : BaseCommandModule {
        private const string MDPath = "Help/";

        [Command("Help")]
        public async Task ShowHelp(CommandContext ctx) {
            var info = new DirectoryInfo(MDPath);
            if (!info.Exists) {
                await ctx.RespondAsync("No help found!");
                return;
            }
            var files = info.GetFiles("*.md");
            var embeds = new List<DiscordEmbed>();
            var total = files.Length;
            var index = 0;
            foreach (var file in files) {
                using (var text = file.OpenText()) {
                    var contents = await text.ReadToEndAsync();
                    var url = string.Empty;
                    if (contents.StartsWith("http")) {
                        //has image
                        var urlIndex = contents.IndexOf('\n');
                        url = contents.Substring(0, urlIndex);
                        contents = contents.Substring(urlIndex);
                    }
                    if (contents.Contains("{")) {
                        var actions = Actions.ActionBase.GetAllActions();
                        var characters = Characters.CharacterBase.GetAllCharacters();

                        foreach (var action in actions) {
                            contents = contents.Replace($"{{{action.GetType().Name}}}", action.GetEmoji());
                        }
                        foreach (var character in characters) {
                            contents = contents.Replace($"{{{character.GetType().Name}}}", character.GetEmoji());
                        }
                    }

                    var embed = new DiscordEmbedBuilder()
                        .WithTitle($"Help - ({++index}/{total})")
                        .WithDescription(contents)
                        .WithColor(DiscordColor.DarkBlue)
                        .WithAuthor("RPG-Bot")
                        .WithFooter("[RPG-Bot] Created by Jeff&Iggy 👋");

                    if (!string.IsNullOrEmpty(url)) {
                        embed.WithImageUrl(url);
                    }
                    embeds.Add(embed);
                }
            }

            index = 0;
            var emojis = new List<DiscordEmoji>() {
                DiscordEmoji.FromName(ctx.Client, ":arrow_backward:"),
                DiscordEmoji.FromName(ctx.Client, ":arrow_forward:"),
            };

            var msg = await ctx.RespondAsync(embed: embeds[index]);
            foreach (var emoji in emojis) {
                await msg.CreateReactionAsync(emoji);
                await Task.Delay(200);
            }

            var sw = Stopwatch.StartNew();
            while (true) {
                //fnmod doesn't exist in C# ffs
                var dirty = false;
                var cc = await msg.GetReactionsAsync(emojis[0]);
                await Task.Delay(200);
                if (cc.Count > 1) {
                    index++;

                    dirty = true;
                } else {
                    cc = await msg.GetReactionsAsync(emojis[1]);
                    await Task.Delay(200);
                    if (cc.Count > 1) {
                        index--;
                        dirty = true;
                    }
                }
                if (dirty) {
                    index -= total * (int)Math.Floor(index / (float)total);
                    await msg.DeleteAllReactionsAsync();
                    await msg.ModifyAsync(embed: embeds[index]);
                    foreach (var emoji in emojis) {
                        await msg.CreateReactionAsync(emoji);
                        await Task.Delay(200);
                    }
                }

                await Task.Delay(500);
                if (sw.Elapsed.TotalMinutes > 5) { break; }
            }
        }
    }
}