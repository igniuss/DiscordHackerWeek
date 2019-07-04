using DiscordBotsList.Api;
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

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly ulong[] BotOwnerIds = new ulong[] { 109706676650663936, 330452192391593987 };
        public static List<GuildOption> GuildOptions;
        public static AuthDiscordBotListApi BotlistAPI { get; private set; }

        public static Permissions[] RequiredPermissions = new Permissions[] {
            Permissions.ReadMessageHistory,
            Permissions.SendMessages,
            Permissions.AddReactions,
            Permissions.UseExternalEmojis,
            Permissions.EmbedLinks,
            Permissions.AccessChannels, //This fucking permission is the one
        };

        #endregion Public Fields

        #region Public Properties

        public static bool RunEvent = true;
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        public static DiscordChannel ImageCache { get; private set; }
        public static DiscordChannel ModerationChannel { get; private set; }
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
            Commands.RegisterCommands<BotlistCommands>();

            Interactivty = Client.UseInteractivity(new InteractivityConfiguration {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Default,
                PollBehaviour = DSharpPlus.Interactivity.Enums.PollBehaviour.Default,
                PaginationDeletion = DSharpPlus.Interactivity.Enums.PaginationDeletion.Default,
            });
            Client.MessageCreated += OnMessageCreated;

            #endregion Commands

            await Client.ConnectAsync();

            if (!string.IsNullOrEmpty(options.DiscordbotToken)) {
                BotlistAPI = new AuthDiscordBotListApi(591408341608038400, options.DiscordbotToken);
            }

            while (true) {
                var now = DateTime.Now;
                var minutes = now.Minute;
                var left = (60 - minutes) % EventInterval;
                await Task.Delay(TimeSpan.FromMinutes(left));
                Logger.Debug("Starting Events");
                if (RunEvent) {
                    var channels = GuildOptions.Select(x => x.GetChannel()).Where(x => x != null);
                    var r = new Random();
                    await StartEvents(channels, r.Next(4, 20));
                }
            }
            await Task.Delay(-1);
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
                if (guild != null) {
                    var prefix = GuildOptions.Where(x => x.Id == guild.Id).FirstOrDefault()?.Prefix;
                    if (!string.IsNullOrEmpty(prefix)) {
                        return prefix;
                    }
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
                                    try {
                                        await channel.SendMessageAsync($"{role.Mention} New quest is starting!");
                                    } catch (System.Exception ex) {
                                        Logger.Error(ex);
                                    }
                                    Logger.Info($"Pinging {role} on {channel.Guild.Name}.");
                                    await Task.Delay(200);
                                }
                            } catch (System.Exception ex) {
                                Logger.Error(ex);
                            }
                        }
                    }
                }
            }
        }

        public static async Task PostLeaderboards(Quest[] quests) {
            try {
                if (quests.Length == 0 || quests == null) { return; }

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
            } catch (System.Exception ex) {
                Logger.Error(ex);
            }
        }

        public async static Task StartEvents(IEnumerable<DiscordChannel> channels, int enemyCount) {
            channels = channels.Where(x => x != null);
            Logger.Info($"Starting event on {channels.Count()} channels with {enemyCount} enemies");
            await PingRoles(channels);
            var questTasks = new List<Task<Quest>>();

            var questGenerator = Generative.QuestGenerator.Instance;
            var questName = questGenerator.GetResult();
            var enemyPaths = new string[enemyCount];
            var enemyNames = new string[enemyCount];
            for (var i = 0; i < enemyCount; i++) {
                enemyPaths[i] = Generative.EnemyGenerator.RandomEnemy();
                enemyNames[i] = Generative.NamesGenerator.Instance.GetResult();
            }

            var questData = new Quest.QuestData {
                QuestName = questName,
                EnemyPaths = enemyPaths,
                BossPath = Generative.EnemyGenerator.RandomEnemy(true),
                EnemyNames = enemyNames,
                BossName = $"Boss {Generative.NamesGenerator.Instance.GetResult()}"
            };
            foreach (var channel in channels) {
                Logger.Info($"Checking permission on {channel.Guild.Name} - {channel.Name}");
                //check permissions
                var member = await channel.Guild.GetMemberAsync(Client.CurrentUser.Id);

                var perm = channel.PermissionsFor(member);

                var hasPermissions = true;
                foreach (var p in RequiredPermissions) {
                    if (!perm.HasPermission(p)) {
                        Logger.Info($"{channel.Guild.Name} - {channel.Name} is missing permissions");
                        hasPermissions = false;
                        break;
                    }
                }
                if (hasPermissions) {
                    var quest = new Quest(questData, channel);
                    Logger.Info($"Starting {quest.QuestName} on {channel.Guild.Name} - {channel.Name}");
                    questTasks.Add(quest.Start());
                }
            }
            if (questTasks.Count == 0) { return; }
            var quests = await Task.WhenAll(questTasks);
            Logger.Info("All Quests completed");
            quests = quests.Where(x => x != null).ToArray();
            if (quests == null || quests.Length == 0) {
                Logger.Warn("Quests was null or empty.");
                return;
            }
            
            Logger.Info("Posting Leaderboards");
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
            Logger.Error(e.Exception);
            await Task.Delay(1);
        }

        private async Task OnClientReady(ReadyEventArgs e) {
            Logger.Info("Client ready");
            await Task.Delay(1);
        }

        private async Task OnGuildAvailable(GuildCreateEventArgs e) {
            if (e.Guild.Id == Options.Guild) {
                Logger.Info("Found options.Guild");
                var channels = await e.Guild.GetChannelsAsync();
                foreach (var channel in channels) {
                    if (ImageCache != null && ModerationChannel != null) {
                        break;
                    }
                    if (channel.Id == Options.ModerationChannel) {
                        Logger.Info("Found moderation channel");
                        ModerationChannel = channel;
                        var moderationTarget = new DiscordChannelTarget(ModerationChannel);
                        NLog.LogManager.Configuration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, moderationTarget);
                    }
                    if (channel.Id == Options.CacheChannel) {
                        Logger.Info("Found cache channel");
                        ImageCache = channel;
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

            Logger.Info("OnGuildAvailable {0}", e.Guild.Name);
            await Task.Delay(1);
        }

        private async Task OnGuildCreated(GuildCreateEventArgs e) {
            var channel = e.Guild.GetDefaultChannel();
            try {
                await channel.SendMessageAsync("Thank you for inviting RPG Bot! Here are some tips to get you started. My default prefix is ``!!`` and can be changed by any admin of the server by using the command ``!!prefix newPrefix``. Adventures begin at the start of every hour. Currently, the adventures will show up in this channel. Admins can change the channel by using the command ``!!setchannel #otherchannel`` or by saying ``!!setchannel`` in the channel they wish to use. If you need any help, have suggestions or bugs to report, or just want to chat, you can join our support server here -> https://discord.gg/VMBn2yV");
            } catch (System.Exception ex) {
                Logger.Error(ex);
                var owner = channel.Guild.Owner;
                if (owner != null) {
                    Logger.Info($"Sent the message to {owner.Mention} because channel.SendMessageAsync didn't work!");
                    await owner.SendMessageAsync("Thank you for inviting RPG Bot! Here are some tips to get you started.My default prefix is ``!!`` and can be changed by any admin of the server by using the command ``!!prefix newPrefix``. Adventures begin at the start of every hour. Currently, the adventures will show up in this channel.Admins can change the channel by using the command ``!!setchannel #otherchannel`` or by saying ``!!setchannel`` in the channel they wish to use. If you need any help, have suggestions or bugs to report, or just want to chat, you can join our support server here -> https://discord.gg/VMBn2yV");
                } else {
                    Logger.Error("Couldn't post OnGuildCreated message, and owner was null...");
                }
            }
        }

        private async Task OnGuildUnavailable(GuildDeleteEventArgs e) {
            Logger.Warn($"{e.Guild.Name} became unavailable");
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
                    Logger.Info($"[{e.Guild.Name}]{e.Author.Username} is trying to execute {command.Name}");
                    try {
                        var ctx = Commands.CreateContext(e.Message, prefix, command, rawArgs);
                        _ = Commands.ExecuteCommandAsync(ctx);
                    } catch (System.Exception ex) {
                        Logger.Error(ex);
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