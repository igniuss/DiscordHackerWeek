using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using LiteDB;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using RPGBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Commands {

    public class ModeratorCommands : BaseCommandModule {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public class TestVariables {
            public DiscordMessage Message { get; set; }
            public DiscordChannel Channel { get; set; }
            public DiscordGuild Guild { get; set; }
            public DiscordUser User { get; set; }
            public DiscordMember Member { get; set; }
            public CommandContext Context { get; set; }

            public TestVariables(DiscordMessage msg, DiscordClient client, CommandContext ctx) {
                this.Client = client;

                Message = msg;
                Channel = msg.Channel;
                Guild = Channel.Guild;
                User = Message.Author;
                if (Guild != null) {
                    Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                Context = ctx;
                Logger.Info($"Created new TestVariables");
            }

            public DiscordClient Client;
        }
        [Command("throw")]
        public async Task TestExceptions(CommandContext ctx) {
            if (!Bot.BotOwnerIds.Contains(ctx.User.Id)) { return; }
            Logger.Error("Test Error Message");
            Logger.Fatal("Fatal Message");
        }

        [Command("eval"), Aliases("evalcs", "cseval", "roslyn"), Description("Evaluates C# code.")]
        public async Task EvalCS(CommandContext ctx, [RemainingText] string code) {
            if (!Bot.BotOwnerIds.Contains(ctx.User.Id)) { return; }
            var msg = ctx.Message;

            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.LastIndexOf("```");

            if (cs1 == -1 || cs2 == -1) {
                throw new ArgumentException("You need to wrap the code into a code block.");
            }

            var cs = code.Substring(cs1, cs2 - cs1);

            msg = await ctx.RespondAsync("", embed: new DiscordEmbedBuilder()
                .WithColor(new DiscordColor("#FF007F"))
                .WithDescription("Evaluating...")
                .Build()).ConfigureAwait(false);

            try {
                var globals = new TestVariables(ctx.Message, ctx.Client, ctx);

                var sopts = ScriptOptions.Default;
                sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext", "DSharpPlus.Interactivity");
                sopts = sopts.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

                var script = CSharpScript.Create(cs, sopts, typeof(TestVariables));
                script.Compile();
                var result = await script.RunAsync(globals).ConfigureAwait(false);

                if (result != null && result.ReturnValue != null && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString())) {
                    await msg.ModifyAsync(embed: new DiscordEmbedBuilder { Title = "Evaluation Result", Description = result.ReturnValue.ToString(), Color = new DiscordColor("#007FFF") }.Build()).ConfigureAwait(false);
                } else {
                    await msg.ModifyAsync(embed: new DiscordEmbedBuilder { Title = "Evaluation Successful", Description = "No result was returned.", Color = new DiscordColor("#007FFF") }.Build()).ConfigureAwait(false);
                }
            } catch (Exception ex) {
                await msg.ModifyAsync(embed: new DiscordEmbedBuilder { Title = "Evaluation Failure", Description = string.Concat("**", ex.GetType().ToString(), "**: ", ex.Message), Color = new DiscordColor("#FF0000") }.Build()).ConfigureAwait(false);
            }
        }

        [Command("toggleevent")]
        public async Task ToggleEvent(CommandContext ctx) {
            if (Bot.BotOwnerIds.Contains(ctx.User.Id)) {
                Bot.RunEvent = !Bot.RunEvent;
                await ctx.RespondAsync($"Turned {(Bot.RunEvent ? "**ON**" : "**OFF**")} events!");
            }
        }

        [Command("setchannel")]
        public async Task SetChannel(CommandContext ctx, DiscordChannel channel = null) {
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator) || Bot.BotOwnerIds.Contains(ctx.User.Id)) {
                if (channel == null) {
                    channel = ctx.Channel;
                }
                if (channel.Type == DSharpPlus.ChannelType.Text) {
                    var member = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
                    //TODO BROKEN, WAIT FOR DSHARPPLUS TO FIX THIS GARBAGE
                    var perm = channel.PermissionsFor(member);
                    var requiredPermissions = new Permissions[] {
                        Permissions.ReadMessageHistory,
                        Permissions.SendMessages,
                        Permissions.AddReactions,
                        Permissions.UseExternalEmojis,
                        Permissions.EmbedLinks,
                        Permissions.AccessChannels,
                    };
                    foreach (var p in requiredPermissions) {
                        if (!perm.HasPermission(p)) {
                            await ctx.RespondAsync($"I'm missing some permissions to work there! I need these permissions to work properly\n{string.Join("\n", requiredPermissions.Select(x => x.ToPermissionString()))}");
                            return;
                        }
                    }
                    var guild = Bot.GuildOptions.Find(x => x.Id == ctx.Guild.Id);
                    if (guild == null) {
                        guild = new GuildOption { Id = ctx.Guild.Id };
                    }
                    guild.Channel = channel.Id;
                    try {
                        DB.Upsert(GuildOption.DBName, GuildOption.TableName, guild);
                        Bot.UpdateGuildOptions();
                        await ctx.RespondAsync($"👌 New default channel is now {guild.GetChannel()}");
                    } catch (System.Exception ex) {
                        Logger.Error(ex);
                    }
                } else {
                    await ctx.RespondAsync("Seems like I can't post messages, or add reactions in that channel!");
                    return;
                }
            } else {
                await ctx.RespondAsync("This command can only be used by server admins.");
            }
        }

        [Command("prefix")]
        [Description("Set the prefix")]
        public async Task SetPrefix(CommandContext ctx, string prefix) {
            // allow admins or bot owners to change the prefix
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator) || Bot.BotOwnerIds.Contains(ctx.User.Id)) {
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
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator) || Bot.BotOwnerIds.Contains(ctx.User.Id)) {
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
            if (Bot.BotOwnerIds.Contains(ctx.User.Id)) {
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

        [Command("dump-db")]
        public async Task DumpDB(CommandContext ctx) {
            if (Bot.BotOwnerIds.Contains(ctx.User.Id)) {
                foreach (var guild in ctx.Client.Guilds) {
                    Logger.Info($"Dumping {guild.Value.Name}");
                    var path = $"{guild.Key}.db";
                    if (!File.Exists(path)) { continue; }
                    try {
                        using (var db = new LiteDatabase(path)) {
                            var collection = db.GetCollection<Player>("players");
                            var players = collection.FindAll();
                            var json = JsonConvert.SerializeObject(players,
                                Formatting.None,
                                new JsonSerializerSettings {
                                    NullValueHandling = NullValueHandling.Ignore,
                                    DefaultValueHandling = DefaultValueHandling.Ignore,
                                });

                            var jsonFile = $"{guild.Value.Name}_{guild.Key}.json";
                            File.WriteAllText(jsonFile, json);
                            await ctx.RespondWithFileAsync(jsonFile);
                            File.Delete(jsonFile);
                            await Task.Delay(300);
                        }
                    } catch (System.Exception ex) {
                        Logger.Error(ex);
                    }
                }
            }
        }

        [Command("event")]
        public async Task RunEvent(CommandContext ctx, int enemyCount = 1, bool onlyHere = true) {
            if (Bot.BotOwnerIds.Contains(ctx.User.Id)) {
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