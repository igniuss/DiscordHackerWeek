using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using RPGBot.Commands;
using RPGBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot {

    public class Bot {

        #region Public Fields

        public static readonly ulong[] BotOwnerIds = new ulong[] { 109706676650663936, 330452192391593987 };
        public static List<GuildOption> GuildOptions;

        #endregion Public Fields

        #region Public Properties

        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        public static DiscordChannel ImageCache { get; private set; }
        public static InteractivityExtension Interactivty { get; private set; }
        public Options Options { get; private set; }

        #endregion Public Properties

        #region Public Constructors

        public Bot() {
            UpdateGuildOptions();
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task RunAsync(Options options) {
            Client = new DiscordClient(new DiscordConfiguration {
                Token = options.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                ReconnectIndefinitely = true,
                LogLevel = LogLevel.Info,
                UseInternalLogHandler = true,
            });
            Options = options;

            #region EVENTS

            Client.Ready += OnClientReady;
            Client.ClientErrored += OnClientErrored;
            Client.GuildAvailable += OnGuildAvailable;
            Client.GuildUnavailable += OnGuildUnavailable;
            Client.Heartbeated += OnHeartbeat;
            Client.SocketClosed += OnSocketClosed;
            Client.GuildCreated += OnGuildCreated;

            #endregion EVENTS

            #region Commands

            Commands = Client.UseCommandsNext(new CommandsNextConfiguration {
                EnableDms = true,
                EnableMentionPrefix = true,
                CaseSensitive = false,
                IgnoreExtraArguments = false,
                UseDefaultCommandHandler = false,
                EnableDefaultHelp = false,
            });

            Commands.RegisterCommands<ModeratorCommands>();
            Commands.RegisterCommands<RPGCommands>();
            Commands.RegisterCommands<HelpCommands>();

            Interactivty = Client.UseInteractivity(new InteractivityConfiguration {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Default,
                PollBehaviour = DSharpPlus.Interactivity.Enums.PollBehaviour.Default,
                PaginationDeletion = DSharpPlus.Interactivity.Enums.PaginationDeletion.Default,
            });
            Client.MessageCreated += OnMessageCreated;

            #endregion Commands

            await Client.ConnectAsync();

            await Task.Delay(-1);
            while (true) {
                var now = DateTime.Now;
                var minutes = now.Minute;
                var left = minutes % EventInterval;
                await Task.Delay(TimeSpan.FromMinutes(left));
                Console.WriteLine("STARTING EVENTS");
                var channels = GuildOptions.Select(x => x.GetChannel());
                var r = new Random();
                await StartEvents(channels, r.Next(4, 20));
            }
        }

        #endregion Public Methods

        #region Private Fields

        //public Timer PeriodicEvent { get; private set; }
        private const int EventInterval = 60;

        #endregion Private Fields

        #region Public Static Methods

        public static DiscordEmbedBuilder GetDefaultEmbed() {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("[RPG-Bot]")
                .WithColor(DiscordColor.MidnightBlue)
                .WithFooter("[RPG-Bot] Made by Iggy & Jeff👏");
            return embed;
        }

        public static string GetPrefix(DiscordGuild guild) {
            if (GuildOptions != null) {
                var prefix = GuildOptions.Where(x => x.Id == guild.Id).FirstOrDefault()?.Prefix;
                if (!string.IsNullOrEmpty(prefix)) {
                    return prefix;
                }
            }
#if DEBUG
            return "xx";
#else
            return "!!";
#endif
        }

        public async static Task PingRoles(IEnumerable<DiscordChannel> channels) {
            foreach (var channel in channels) {
                var option = GuildOptions.FirstOrDefault(x => x.GetChannel() == channel);
                if (option != null) {
                    if (option.RoleId != 0) {
                        var role = channel.Guild.GetRole(option.RoleId);
                        if (role != null) {
                            try {
                                if (role.IsMentionable) {
                                    await channel.SendMessageAsync($"{role.Mention} New quest is starting!");
                                    Console.WriteLine($"Pinging {role} on {channel.Guild.Name}.");
                                    await Task.Delay(200);
                                }
                            } catch (System.Exception ex) {
                                Console.WriteLine(ex);
                            }
                        }
                    }
                }
            }
        }

        public static async Task PostLeaderboards(Quest[] quests) {
            if (quests.Length == 0) { return; }
            var count = 0;
            var clearedQuests = quests.Where(x => x.Success);
            var questName = quests.First().QuestName;
            var embed = Bot.GetDefaultEmbed()
            .WithImageUrl("https://i.imgur.com/vVukN4y.png")
            .AddField("__Quest__", questName);

            if (clearedQuests.Count() > 0) {
                embed = embed.AddField("**Ranks**", string.Join("\n", clearedQuests.Take(3).Select(x => $"{++count}) {x.Channel.Guild.Name} - {x.CompletedTime.ToString(@"mm\:ss")}")));
            }
            embed = embed.AddField("Event Stats", $"{quests.Count()} Guilds joined the event\n{quests.Where(x => x.Success).Count()} Guilds completed the event.");

            foreach (var quest in quests) {
                await quest.Channel.SendMessageAsync(embed: embed);
                await Task.Delay(100);
            }
        }

        public async static Task StartEvents(IEnumerable<DiscordChannel> channels, int enemyCount) {
            await PingRoles(channels);
            var questTasks = new List<Task<Quest>>();

            var questGenerator = new Generative.QuestGenerator();
            var questName = questGenerator.GetResult();
            var enemies = new string[enemyCount];

            for (var i = 0; i < enemyCount; i++) {
                enemies[i] = Generative.EnemyGenerator.RandomEnemy();
            }

            foreach (var channel in channels) {
                var quest = new Quest(channel, questName, enemies, Generative.EnemyGenerator.RandomEnemy(true));
                questTasks.Add(quest.Start());
            }

            var quests = await Task.WhenAll(questTasks);

            if (quests == null || quests.Length == 0) {
                Console.WriteLine("Quests was null or empty");
                return;
            }
            await PostLeaderboards(quests);
        }

        public static void UpdateGuildOptions() {
            GuildOptions = LoadGuildOptions();
        }

        #endregion Public Static Methods

        #region Private Methods

        private static List<GuildOption> LoadGuildOptions() {
            var options = DB.GetAll<GuildOption>(GuildOption.DBName, GuildOption.TableName).ToList();
            if (options == null) {
                options = new List<GuildOption>();
                DB.Insert(GuildOption.DBName, GuildOption.TableName, options);
            }
            return options;
        }

        private void Log(LogLevel level, string msg) {
            Client.DebugLogger.LogMessage(level, "RPG-Bot", msg, DateTime.Now);
        }

        private async Task OnHeartbeat(HeartbeatEventArgs e) {
            //calculate time left till new mission
            var timeLeft = TimeSpan.FromMinutes(60 - DateTime.Now.Minute);

            //get all the other data in here boys
            var serverCount = Bot.Client.Guilds.Count;
            var memberCount = Bot.Client.Guilds.Sum(x => x.Value.MemberCount);

            var activity = new DiscordActivity($"⚔ {Math.Floor(timeLeft.TotalMinutes)} minutes until next event.    [{serverCount} servers with {memberCount} members]", ActivityType.Streaming);

            await Bot.Client.UpdateStatusAsync(activity, UserStatus.Online);
        }

        #endregion Private Methods

        #region Event Callbacks

        private async Task OnClientErrored(ClientErrorEventArgs e) {
            Log(LogLevel.Error, e.Exception.ToString());
            await Task.Delay(1);
        }

        private async Task OnClientReady(ReadyEventArgs e) {
            Log(LogLevel.Info, "Client Ready");
            await Task.Delay(1);
        }

        private async Task OnGuildAvailable(GuildCreateEventArgs e) {
            if (e.Guild.Id == Options.Guild) {
                Log(LogLevel.Info, $"Setting up ImageCache");
                var channels = await e.Guild.GetChannelsAsync();
                foreach (var channel in channels) {
                    if (channel.Id == Options.CacheChannel) {
                        ImageCache = channel;
                        break;
                    }
                }
            }

            if (!GuildOptions.Any(x => x.Id == e.Guild.Id)) {
                var options = new GuildOption {
                    Id = e.Guild.Id,
                    Channel = e.Guild.GetDefaultChannel().Id
                };

                DB.Insert(GuildOption.DBName, GuildOption.TableName, options);
                LoadGuildOptions();
            }

            Log(LogLevel.Info, $"{e.Guild.Name} is now Available");
            await Task.Delay(1);
        }

        private async Task OnGuildCreated(GuildCreateEventArgs e) {
            var channel = e.Guild.GetDefaultChannel();
            await channel.SendMessageAsync("Thank you for inviting RPG Bot! Here are some tips to get you started. My default prefix is ``!!`` and can be changed by any admin of the server by using the command ``!!prefix newPrefix``. Adventures begin at the start of every hour. Currently, the adventures will show up in this channel. Admins can change the channel by using the command ``!!setchannel #otherchannel`` or by saying ``!!setchannel`` in the channel they wish to use. If you need any help, have suggestions or bugs to report, or just want to chat, you can join our support server here -> https://discord.gg/VMBn2yV");
        }

        private async Task OnGuildUnavailable(GuildDeleteEventArgs e) {
            Log(LogLevel.Warning, $"{e.Guild.Name} is now Unavailable");
            await Task.Delay(1);
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e) {
            if (e.Author.IsBot) { return; }
            await Task.Delay(10);
            var prefix = GetPrefix(e.Guild);
            if (e.Message.Content.StartsWith(prefix)) {
                var cmdText = e.Message.Content.Substring(prefix.Length);
                var command = Commands.FindCommand(cmdText, out var rawArgs);
                if (command != null) {
                    try {
                        var ctx = Commands.CreateContext(e.Message, prefix, command, rawArgs);
                        _ = Commands.ExecuteCommandAsync(ctx);
                    } catch (System.Exception ex) {
                        Log(LogLevel.Error, ex.ToString());
                    }
                }
            }
        }

        private async Task OnSocketClosed(SocketCloseEventArgs e) {
            await e.Client.ReconnectAsync(true);
        }

        #endregion Event Callbacks
    }
}